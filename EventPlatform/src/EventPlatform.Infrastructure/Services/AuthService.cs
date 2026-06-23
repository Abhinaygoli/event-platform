using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EventPlatform.Application.DTOs.Auth;
using EventPlatform.Domain.Entities;
using EventPlatform.Domain.Enums;
using EventPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventPlatform.Application.Interfaces;

namespace EventPlatform.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        //Register
        public async Task<(AuthResponseDto response, string refreshToken)> RegisterAsync(RegisterDto dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email.ToLower()))
                throw new InvalidOperationException("Email already registered.");

            if (!Enum.TryParse<UserRole>(dto.Role, true, out var role))
                role = UserRole.Attendee;

            var user = new User
            {
                Name = dto.Name.Trim(),
                Email = dto.Email.ToLower().Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = role
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return await GenerateTokensAsync(user);
        }

        //Login 
        public async Task<(AuthResponseDto response, string refreshToken)> LoginAsync(LoginDto dto)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower())
                ?? throw new UnauthorizedAccessException("Invalid email or password.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid email or password.");

            return await GenerateTokensAsync(user);
        }

        // ── Refresh with rotation + reuse detection ───────────────
        public async Task<(AuthResponseDto response, string newRefreshToken)> RefreshTokenAsync(
            string refreshToken)
        {
            // Look for a user with this exact token that is still valid
            var user = await _db.Users
                .FirstOrDefaultAsync(u =>
                    u.RefreshToken == refreshToken &&
                    u.RefreshTokenExpiry > DateTime.UtcNow);

            if (user == null)
            {
                // Token not found as valid — check if it exists but expired
                // This indicates possible token reuse/theft scenario
                var suspectedUser = await _db.Users
                    .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

                if (suspectedUser != null)
                {
                    // Attacker may have used a stolen token after it expired.
                    // Revoke the entire session to protect the legitimate user.
                    suspectedUser.RefreshToken = null;
                    suspectedUser.RefreshTokenExpiry = null;
                    await _db.SaveChangesAsync();
                }

                throw new UnauthorizedAccessException("Invalid or expired refresh token.");
            }

            // Token is valid — rotate it immediately
            // Old token is overwritten, so it can never be used again
            return await GenerateTokensAsync(user);
        }

        // ── Revoke on logout ──────────────────────────────────────
        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (user == null) return; // already revoked or invalid — safe no-op

            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _db.SaveChangesAsync();
        }

        // ── Core token generation (used by all 3 flows) ───────────
        private async Task<(AuthResponseDto response, string refreshToken)> GenerateTokensAsync(
            User user)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var secretKey = jwtSettings["Secret"]!;
            var issuer = jwtSettings["Issuer"]!;
            var audience = jwtSettings["Audience"]!;
            var expiryMin = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");
            var refreshExpiryDays = int.Parse(jwtSettings["RefreshExpiryDays"] ?? "7");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier,     user.Id.ToString()),
            new Claim(ClaimTypes.Name,               user.Name),
            new Claim(ClaimTypes.Role,               user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
        };

            var expiry = DateTime.UtcNow.AddMinutes(expiryMin);
            var jwtToken = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiry,
                signingCredentials: creds
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);
            var refreshToken = GenerateRefreshToken();

            // Rotation: overwrite old refresh token in DB with new one
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(refreshExpiryDays);
            await _db.SaveChangesAsync();

            // Response DTO — refresh token NOT included here
            var response = new AuthResponseDto
            {
                AccessToken = accessToken,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role.ToString(),
                ExpiresAt = expiry
            };

            return (response, refreshToken);
        }

        private static string GenerateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }
    }
}

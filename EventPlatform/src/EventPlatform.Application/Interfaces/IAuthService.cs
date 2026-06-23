using EventPlatform.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatform.Application.Interfaces
{
    public interface IAuthService
    {
        /// <summary>
        /// Register new user.
        /// Returns access token DTO + raw refresh token string.
        /// Controller sets refresh token as HttpOnly cookie.
        /// </summary>
        Task<(AuthResponseDto response, string refreshToken)> RegisterAsync(RegisterDto dto);

        /// <summary>
        /// Login existing user.
        /// Returns access token DTO + raw refresh token string.
        /// Controller sets refresh token as HttpOnly cookie.
        /// </summary>
        Task<(AuthResponseDto response, string refreshToken)> LoginAsync(LoginDto dto);

        /// <summary>
        /// Validates refresh token and rotates it (generates a new one).
        /// Implements reuse detection — if an expired/already-used token
        /// is presented, the session is fully revoked for security.
        /// Returns new access token DTO + new refresh token for cookie.
        /// </summary>
        Task<(AuthResponseDto response, string newRefreshToken)> RefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Revokes refresh token from DB on logout.
        /// Even if attacker has the cookie value it will no longer work.
        /// </summary>
        Task RevokeRefreshTokenAsync(string refreshToken);
    }
}

using EventPlatform.Application.DTOs.Speakers;
using EventPlatform.Application.Interfaces;
using EventPlatform.Domain.Entities;
using EventPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatform.Infrastructure.Services
{
    public class SpeakerService : ISpeakerService
    {
        private readonly AppDbContext _db;

        public SpeakerService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<SpeakerResponseDto>> GetAllAsync()
        {
            return await _db.Speakers
                .Include(s => s.User)
                .Include(s => s.Sessions)
                .AsNoTracking()
                .Select(s => MapToDto(s))
                .ToListAsync();
        }

        public async Task<SpeakerResponseDto> GetByIdAsync(Guid id)
        {
            var speaker = await _db.Speakers
                .Include(s => s.User)
                .Include(s => s.Sessions)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new KeyNotFoundException("Speaker not found.");

            return MapToDto(speaker);
        }

        public async Task<SpeakerResponseDto> GetByUserIdAsync(Guid userId)
        {
            var speaker = await _db.Speakers
                .Include(s => s.User)
                .Include(s => s.Sessions)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId)
                ?? throw new KeyNotFoundException("Speaker profile not found.");

            return MapToDto(speaker);
        }

        public async Task<SpeakerResponseDto> CreateAsync(CreateSpeakerDto dto, Guid userId)
        {
            // Verify user exists and has Speaker role
            var user = await _db.Users.FindAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            if (await _db.Speakers.AnyAsync(s => s.UserId == userId))
                throw new InvalidOperationException("Speaker profile already exists.");

            var speaker = new Speaker
            {
                UserId = userId,
                Bio = dto.Bio,
                Company = dto.Company,
                Designation = dto.Designation,
                PhotoUrl = dto.PhotoUrl,
                LinkedInUrl = dto.LinkedInUrl
            };

            _db.Speakers.Add(speaker);
            await _db.SaveChangesAsync();
            return await GetByUserIdAsync(userId);
        }

        public async Task<SpeakerResponseDto> UpdateAsync(Guid userId, UpdateSpeakerDto dto)
        {
            var speaker = await _db.Speakers
                .FirstOrDefaultAsync(s => s.UserId == userId)
                ?? throw new KeyNotFoundException("Speaker profile not found.");

            speaker.Bio = dto.Bio;
            speaker.Company = dto.Company;
            speaker.Designation = dto.Designation;
            speaker.PhotoUrl = dto.PhotoUrl;
            speaker.LinkedInUrl = dto.LinkedInUrl;

            await _db.SaveChangesAsync();
            return await GetByUserIdAsync(userId);
        }

        private static SpeakerResponseDto MapToDto(Speaker s) => new()
        {
            Id = s.Id,
            UserId = s.UserId,
            Name = s.User?.Name ?? string.Empty,
            Email = s.User?.Email ?? string.Empty,
            Bio = s.Bio,
            Company = s.Company,
            Designation = s.Designation,
            PhotoUrl = s.PhotoUrl,
            LinkedInUrl = s.LinkedInUrl,
            SessionCount = s.Sessions?.Count ?? 0
        };
    }
}

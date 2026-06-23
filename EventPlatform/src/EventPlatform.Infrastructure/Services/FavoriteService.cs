using EventPlatform.Application.DTOs.Common;
using EventPlatform.Application.DTOs.Favorites;
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
    public class FavoriteService : IFavoriteService
    {
        private readonly AppDbContext _db;

        public FavoriteService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<FavoriteResponseDto>> GetMyFavoritesAsync(Guid userId)
        {
            return await _db.Favorites
                .Include(f => f.Session)
                    .ThenInclude(s => s.Event)
                .Include(f => f.Session)
                    .ThenInclude(s => s.Speaker)
                        .ThenInclude(sp => sp!.User)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .AsNoTracking()
                .Select(f => new FavoriteResponseDto
                {
                    Id = f.Id,
                    SessionId = f.SessionId,
                    SessionTitle = f.Session.Title,
                    EventTitle = f.Session.Event.Title,
                    StartTime = f.Session.StartTime,
                    SpeakerName = f.Session.Speaker != null ? f.Session.Speaker.User.Name : null,
                    Tags = f.Session.Tags,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<MessageResponseDto> AddFavoriteAsync(Guid sessionId, Guid userId)
        {
            if (!await _db.Sessions.AnyAsync(s => s.Id == sessionId))
                throw new KeyNotFoundException("Session not found.");

            var existing = await _db.Favorites
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(f => f.UserId == userId && f.SessionId == sessionId);

            if (existing != null)
            {
                if (!existing.IsDeleted)
                    throw new InvalidOperationException("Session already in your favorites.");

                existing.IsDeleted = false;
                existing.CreatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return new MessageResponseDto("Session added to favorites.");
            }

            _db.Favorites.Add(new Favorite { UserId = userId, SessionId = sessionId });
            await _db.SaveChangesAsync();
            return new MessageResponseDto("Session added to favorites.");
        }

        public async Task<MessageResponseDto> RemoveFavoriteAsync(Guid sessionId, Guid userId)
        {
            var fav = await _db.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.SessionId == sessionId)
                ?? throw new KeyNotFoundException("Session not in favorites.");

            //_db.Favorites.Remove(fav);
            fav.IsDeleted = true;
            await _db.SaveChangesAsync();
            return new MessageResponseDto("Session removed from favorites.");
        }
    }
}

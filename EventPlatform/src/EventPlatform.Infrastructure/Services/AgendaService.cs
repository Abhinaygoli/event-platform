using EventPlatform.Application.DTOs.Agenda;
using EventPlatform.Application.DTOs.Common;
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
    public class AgendaService : IAgendaService
    {
        private readonly AppDbContext _db;

        public AgendaService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<AgendaItemResponseDto>> GetMyAgendaAsync(Guid userId)
        {
            return await _db.AgendaItems
                .Include(a => a.Session)
                    .ThenInclude(s => s.Event)
                .Include(a => a.Session)
                    .ThenInclude(s => s.Speaker)
                        .ThenInclude(sp => sp!.User)
                .Where(a => a.UserId == userId)
                .OrderBy(a => a.Session.StartTime)
                .AsNoTracking()
                .Select(a => new AgendaItemResponseDto
                {
                    Id = a.Id,
                    SessionId = a.SessionId,
                    SessionTitle = a.Session.Title,
                    EventTitle = a.Session.Event.Title,
                    StartTime = a.Session.StartTime,
                    EndTime = a.Session.EndTime,
                    Room = a.Session.Room,
                    SpeakerName = a.Session.Speaker != null ? a.Session.Speaker.User.Name : null,
                    Status = a.Session.Status.ToString(),
                    AddedAt = a.AddedAt
                })
                .ToListAsync();
        }

        public async Task<MessageResponseDto> AddToAgendaAsync(Guid sessionId, Guid userId)
        {
            if (!await _db.Sessions.AnyAsync(s => s.Id == sessionId))
                throw new KeyNotFoundException("Session not found.");

            // Check including soft-deleted rows (IgnoreQueryFilters)
            var existing = await _db.AgendaItems
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.UserId == userId && a.SessionId == sessionId);

            if (existing != null)
            {
                if (!existing.IsDeleted)
                    throw new InvalidOperationException("Session already in your agenda.");

                // Previously removed — restore it instead of inserting new
                existing.IsDeleted = false;
                existing.AddedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return new MessageResponseDto("Session added to your agenda.");
            }

            _db.AgendaItems.Add(new AgendaItem { UserId = userId, SessionId = sessionId });
            await _db.SaveChangesAsync();
            return new MessageResponseDto("Session added to your agenda.");
        }

        public async Task<MessageResponseDto> RemoveFromAgendaAsync(Guid sessionId, Guid userId)
        {
            var item = await _db.AgendaItems
                .FirstOrDefaultAsync(a => a.UserId == userId && a.SessionId == sessionId)
                ?? throw new KeyNotFoundException("Session not in your agenda.");

            //_db.AgendaItems.Remove(item);
            item.IsDeleted = true;
            await _db.SaveChangesAsync();
            return new MessageResponseDto("Session removed from your agenda.");
        }
    }
}

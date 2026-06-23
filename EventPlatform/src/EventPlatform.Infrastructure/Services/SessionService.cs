using EventPlatform.Application.DTOs.Common;
using EventPlatform.Application.DTOs.Sessions;
using EventPlatform.Application.Interfaces;
using EventPlatform.Domain.Entities;
using EventPlatform.Domain.Enums;
using EventPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace EventPlatform.Infrastructure.Services
{
    public class SessionService : ISessionService
    {
        private readonly AppDbContext _db;

        public SessionService(AppDbContext db)
        {
            _db = db;
        }

        // ── Get sessions by event (paged) ─────────────────────────
        public async Task<PagedResultDto<SessionResponseDto>> GetByEventAsync(
            Guid eventId, int page, int pageSize, Guid? userId, string? status)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var query = _db.Sessions
                .Include(s => s.Speaker).ThenInclude(sp => sp!.User)
                .Include(s => s.Event)
                .Include(s => s.AgendaItems)
                .Include(s => s.Favorites)
                .Where(s => s.EventId == eventId)
                .OrderBy(s => s.StartTime)
                .AsNoTracking();
            var total = await query.CountAsync();

            if (!string.IsNullOrEmpty(status))
            {
                var now = DateTime.UtcNow;
                if (status == "upcoming")
                {
                    query = query.Where(
                      e => e.StartTime > now
                    );
                }
                if (status == "past")
                {
                    query = query.Where(
                      e => e.EndTime < now
                    );
                }
                if (status == "live")
                {
                    query = query.Where(
                      e => e.StartTime <= now &&
                           e.EndTime >= now
                    );
                }
                if (status == "cancelled")
                {
                    query = query.Where(
                      e => e.Status == SessionStatus.Cancelled
                    );
                }
            }

            var sessions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // user-specific flags
            HashSet<Guid> agendaIds = new();
            HashSet<Guid> favoriteIds = new();

            if (userId.HasValue)
            {
                agendaIds = (await _db.AgendaItems
                    .Where(a => a.UserId == userId.Value)
                    .Select(a => a.SessionId).ToListAsync()).ToHashSet();

                favoriteIds = (await _db.Favorites
                    .Where(f => f.UserId == userId.Value)
                    .Select(f => f.SessionId).ToListAsync()).ToHashSet();
            }

            return new PagedResultDto<SessionResponseDto>
            {
                Items = sessions.Select(s => MapToDto(s, agendaIds.Contains(s.Id),
                                                              favoriteIds.Contains(s.Id))).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Get sessions for a specific speaker (paged) with optional status filter.
        /// </summary>
        public async Task<PagedResultDto<SessionResponseDto>> GetMySessionsAsync(
                int page,
                int pageSize,
                Guid? speakerId,
                string? status)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var query = _db.Sessions
                .Include(s => s.Speaker)
                    .ThenInclude(sp => sp!.User)
                .Include(s => s.Event)
                .Include(s => s.AgendaItems)
                .Include(s => s.Favorites)
                .Where(s => s.SpeakerId == speakerId)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(status))
            {
                var now = DateTime.UtcNow;
                if (status == "upcoming")
                {
                    query = query.Where(s =>
                        s.StartTime > now);
                }
                if (status == "live")
                {
                    query = query.Where(s =>
                        s.StartTime <= now &&
                        s.EndTime >= now);
                }
                if (status == "past")
                {
                    query = query.Where(s =>
                        s.EndTime < now);
                }
                if (status == "cancelled")
                {
                    query = query.Where(s =>
                        s.Status == SessionStatus.Cancelled);
                }
            }
            var total = await query.CountAsync();
            var sessions = await query
                .OrderBy(s => s.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResultDto<SessionResponseDto>
            {
                Items = sessions.Select(s => MapToDto(s,false,false)).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        // ── Get single session ────────────────────────────────────
        public async Task<SessionResponseDto> GetByIdAsync(Guid id, Guid? userId)
        {
            var s = await _db.Sessions
                .Include(s => s.Speaker).ThenInclude(sp => sp!.User)
                .Include(s => s.Event)
                .Include(s => s.AgendaItems)
                .Include(s => s.Favorites)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new KeyNotFoundException("Session not found.");

            bool inAgenda = userId.HasValue && s.AgendaItems.Any(a => a.UserId == userId.Value);
            bool isFavorite = userId.HasValue && s.Favorites.Any(f => f.UserId == userId.Value);

            return MapToDto(s, inAgenda, isFavorite);
        }

        // ── Create session (Admin) ────────────────────────────────
        public async Task<SessionResponseDto> CreateAsync(CreateSessionDto dto)
        {
            if (!await _db.Events.AnyAsync(e => e.Id == dto.EventId))
                throw new KeyNotFoundException("Event not found.");

            if (dto.SpeakerId.HasValue &&
                !await _db.Speakers.AnyAsync(sp => sp.Id == dto.SpeakerId))
                throw new KeyNotFoundException("Speaker not found.");

            if (dto.EndTime <= dto.StartTime)
                throw new ArgumentException("End time must be after start time.");

            var session = new Session
            {
                EventId = dto.EventId,
                SpeakerId = dto.SpeakerId,
                Title = dto.Title.Trim(),
                Description = dto.Description,
                Tags = dto.Tags,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Room = dto.Room,
                MaxCapacity = dto.MaxCapacity,
                Status = SessionStatus.Scheduled
            };

            _db.Sessions.Add(session);
            await _db.SaveChangesAsync();

            // reload with includes
            return await GetByIdAsync(session.Id, null);
        }

        // ── Update session (Admin) ────────────────────────────────
        public async Task<SessionResponseDto> UpdateAsync(Guid id, UpdateSessionDto dto)
        {
            var session = await _db.Sessions.FindAsync(id)
                ?? throw new KeyNotFoundException("Session not found.");

            if (dto.SpeakerId.HasValue &&
                !await _db.Speakers.AnyAsync(sp => sp.Id == dto.SpeakerId))
                throw new KeyNotFoundException("Speaker not found.");

            if (dto.EndTime <= dto.StartTime)
                throw new ArgumentException("End time must be after start time.");

            session.SpeakerId = dto.SpeakerId;
            session.Title = dto.Title.Trim();
            session.Description = dto.Description;
            session.Tags = dto.Tags;
            session.StartTime = dto.StartTime;
            session.EndTime = dto.EndTime;
            session.Room = dto.Room;
            session.MaxCapacity = dto.MaxCapacity;

            await _db.SaveChangesAsync();
            return await GetByIdAsync(session.Id, null);
        }

        // ── Update session status (Admin) ─────────────────────────
        public async Task<MessageResponseDto> UpdateStatusAsync(Guid id, string status)
        {
            var session = await _db.Sessions.FindAsync(id)
                ?? throw new KeyNotFoundException("Session not found.");

            if (!Enum.TryParse<SessionStatus>(status, true, out var parsed))
                throw new ArgumentException($"Invalid status. Valid: Scheduled, Live, Completed, Cancelled");

            session.Status = parsed;
            await _db.SaveChangesAsync();
            return new MessageResponseDto($"Session status updated to {parsed}.");
        }

        // ── Delete session (Admin) ────────────────────────────────
        public async Task DeleteAsync(Guid id)
        {
            var session = await _db.Sessions
                .Include(s => s.AgendaItems)
                .Include(s => s.Favorites)
                .FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new KeyNotFoundException("Session not found.");

            // Reuse the EXACT same cascade logic EventService uses —
            // defined once in SessionCascadeHelper, never duplicated
            SessionCascadeHelper.MarkSessionDeleted(session);

            await _db.SaveChangesAsync();
        }

        // ── Mapping helper ────────────────────────────────────────
        private static SessionResponseDto MapToDto(Session s, bool inAgenda, bool isFavorite) => new()
        {
            Id = s.Id,
            EventId = s.EventId,
            EventTitle = s.Event?.Title ?? string.Empty,
            SpeakerId = s.SpeakerId,
            SpeakerName = s.Speaker?.User?.Name,
            SpeakerCompany = s.Speaker?.Company,
            SpeakerPhotoUrl = s.Speaker?.PhotoUrl,
            Title = s.Title,
            Description = s.Description,
            Tags = s.Tags,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            Room = s.Room,
            //Status = s.Status.ToString(),
            Status = GetDynamicStatus(s).ToString(),
            MaxCapacity = s.MaxCapacity,
            AgendaCount = s.AgendaItems?.Count ?? 0,
            FavoriteCount = s.Favorites?.Count ?? 0,
            IsInAgenda = inAgenda,
            IsFavorited = isFavorite,
            CreatedAt = s.CreatedAt
        };

        /// <summary>
        /// Computes the display status from current UTC time.
        /// Cancelled is the ONLY status that comes from DB — it is an
        /// explicit Admin override that time cannot undo automatically.
        /// All other statuses are derived from StartTime and EndTime.
        /// </summary>
        private static SessionStatus GetDynamicStatus(Session session)
        {
            // Admin explicitly cancelled this session — respect that override
            // regardless of what the time says
            if (session.Status == SessionStatus.Cancelled)
                return SessionStatus.Cancelled;

            var now = DateTime.UtcNow;

            // Session hasn't started yet
            if (now < session.StartTime)
                return SessionStatus.Scheduled;

            // Session is currently running
            if (now >= session.StartTime && now <= session.EndTime)
                return SessionStatus.Live;

            // Session has finished
            return SessionStatus.Completed;
        }
    }
}

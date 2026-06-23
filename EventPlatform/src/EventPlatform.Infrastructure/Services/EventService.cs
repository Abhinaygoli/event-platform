using EventPlatform.Application.DTOs.Common;
using EventPlatform.Application.DTOs.Events;
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
    public class EventService : IEventService
    {
        private readonly AppDbContext _db;

        public EventService(AppDbContext db)
        {
            _db = db;
        }

        // ── Get all events (paged) ────────────────────────────────
        public async Task<PagedResultDto<EventResponseDto>> GetAllAsync(
            int page, int pageSize, Guid? currentUserId, string? status)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var query = _db.Events
                .Include(e => e.Sessions)
                .Include(e => e.Registrations)
                .OrderByDescending(e => e.StartDate)
                .AsNoTracking();

            var total = await query.CountAsync();
            var sessionsCount = await query.Select(e => e.Sessions.Count).ToListAsync();

            if (!string.IsNullOrEmpty(status))
            {
                var now = DateTime.UtcNow;
                if (status == "upcoming")
                {
                    query = query.Where(
                      e => e.StartDate > now
                    );
                }
                if (status == "past")
                {
                    query = query.Where(
                      e => e.EndDate < now
                    );
                }
                if (status == "live")
                {
                    query = query.Where(
                      e => e.StartDate <= now &&
                           e.EndDate >= now
                    );
                }
            }

            var events = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // get registered event IDs for current user
            HashSet<Guid> registeredIds = new();
            if (currentUserId.HasValue)
            {
                registeredIds = (await _db.Registrations
                    .Where(r => r.UserId == currentUserId.Value)
                    .Select(r => r.EventId)
                    .ToListAsync()).ToHashSet();
            }

            return new PagedResultDto<EventResponseDto>
            {
                Items = events.Select(e => MapToDto(e, registeredIds.Contains(e.Id))).ToList(),
                TotalCount = total,
                SessionsCount = sessionsCount.Sum(),
                Page = page,
                PageSize = pageSize
            };
        }

        // ── Get all event summaries (public) ───────────────────────
        public async Task<List<EventSummaryDto>> GetAllSummariesAsync()
        {
            var events = await _db.Events
                .AsNoTracking()
                .Select(e => new EventSummaryDto
                {
                    Id = e.Id,
                    Title = e.Title
                })
                .ToListAsync();
            return events;
        }

        // ── Get single event ──────────────────────────────────────
        public async Task<EventResponseDto> GetByIdAsync(Guid id, Guid? currentUserId)
        {
            var ev = await _db.Events
                .Include(e => e.Sessions)
                .Include(e => e.Registrations)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new KeyNotFoundException("Event not found.");

            bool isRegistered = currentUserId.HasValue &&
                ev.Registrations.Any(r => r.UserId == currentUserId.Value);

            return MapToDto(ev, isRegistered);
        }

        // ── Create event (Admin) ──────────────────────────────────
        public async Task<EventResponseDto> CreateAsync(CreateEventDto dto, Guid adminId)
        {
            if (dto.EndDate <= dto.StartDate)
                throw new ArgumentException("End date must be after start date.");

            var ev = new Event
            {
                Title = dto.Title.Trim(),
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Location = dto.Location,
                BannerUrl = dto.BannerUrl,
                CreatedBy = adminId
            };

            _db.Events.Add(ev);
            await _db.SaveChangesAsync();
            return MapToDto(ev, false);
        }

        // ── Update event (Admin) ──────────────────────────────────
        public async Task<EventResponseDto> UpdateAsync(Guid id, UpdateEventDto dto, Guid adminId)
        {
            var ev = await _db.Events
                .Include(e => e.Sessions)
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new KeyNotFoundException("Event not found.");

            if (dto.EndDate <= dto.StartDate)
                throw new ArgumentException("End date must be after start date.");

            ev.Title = dto.Title.Trim();
            ev.Description = dto.Description;
            ev.StartDate = dto.StartDate;
            ev.EndDate = dto.EndDate;
            ev.Location = dto.Location;
            ev.BannerUrl = dto.BannerUrl;

            await _db.SaveChangesAsync();
            return MapToDto(ev, false);
        }

        // ── Delete event — FULL CASCADE SOFT DELETE (Admin) ──────────────────────────────────
        public async Task DeleteAsync(Guid id)
        {
            var ev = await _db.Events
            .Include(e => e.Sessions)
                .ThenInclude(s => s.AgendaItems)
            .Include(e => e.Sessions)
                .ThenInclude(s => s.Favorites)
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new KeyNotFoundException("Event not found.");

            // Mark the event itself
            ev.IsDeleted = true;

            // Mark all registrations for this event
            foreach (var registration in ev.Registrations)
                registration.IsDeleted = true;

            // Cascade each session using the SHARED helper —
            // the exact same logic SessionService.DeleteAsync uses
            foreach (var session in ev.Sessions)
                SessionCascadeHelper.MarkSessionDeleted(session);

            await _db.SaveChangesAsync();
        }

        // ── Register attendee to event ────────────────────────────
        public async Task<MessageResponseDto> RegisterAsync(Guid eventId, Guid userId)
        {
            if (!await _db.Events.AnyAsync(e => e.Id == eventId))
                throw new KeyNotFoundException("Event not found.");

            if (await _db.Registrations.AnyAsync(r => r.UserId == userId && r.EventId == eventId))
                throw new InvalidOperationException("Already registered for this event.");

            if (await _db.Events.AnyAsync(e => e.Id == eventId && DateTime.UtcNow > e.EndDate))
                throw new InvalidOperationException("Registration closed. Event already completed.");

            _db.Registrations.Add(new Registration { UserId = userId, EventId = eventId });
            await _db.SaveChangesAsync();
            return new MessageResponseDto("Successfully registered for event.");
        }

        // ── Unregister attendee from event ────────────────────────
        public async Task<MessageResponseDto> UnregisterAsync(Guid eventId, Guid userId)
        {
            var reg = await _db.Registrations
                .FirstOrDefaultAsync(r => r.UserId == userId && r.EventId == eventId)
                ?? throw new KeyNotFoundException("Registration not found.");

            //_db.Registrations.Remove(reg);
            reg.IsDeleted = true;
            await _db.SaveChangesAsync();
            return new MessageResponseDto("Successfully unregistered from event.");
        }

        // ── Mapping helper ────────────────────────────────────────
        private static EventResponseDto MapToDto(Event ev, bool isRegistered) => new()
        {
            Id = ev.Id,
            Title = ev.Title,
            Description = ev.Description,
            StartDate = ev.StartDate,
            EndDate = ev.EndDate,
            Location = ev.Location,
            BannerUrl = ev.BannerUrl,
            CreatedBy = ev.CreatedBy,
            CreatedAt = ev.CreatedAt,
            SessionCount = ev.Sessions?.Count ?? 0,
            RegistrationCount = ev.Registrations?.Count ?? 0,
            IsRegistered = isRegistered
        };
    }
}

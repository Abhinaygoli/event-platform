using EventPlatform.Application.DTOs.Common;
using EventPlatform.Application.DTOs.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatform.Application.Interfaces
{
    public interface IEventService
    {
        Task<PagedResultDto<EventResponseDto>> GetAllAsync(int page, int pageSize, Guid? currentUserId, string? status);
        Task<List<EventSummaryDto>> GetAllSummariesAsync();
        Task<EventResponseDto> GetByIdAsync(Guid id, Guid? currentUserId);
        Task<EventResponseDto> CreateAsync(CreateEventDto dto, Guid adminId);
        Task<EventResponseDto> UpdateAsync(Guid id, UpdateEventDto dto, Guid adminId);
        Task DeleteAsync(Guid id);
        Task<MessageResponseDto> RegisterAsync(Guid eventId, Guid userId);
        Task<MessageResponseDto> UnregisterAsync(Guid eventId, Guid userId);
    }
}

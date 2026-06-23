using EventPlatform.Application.DTOs.Common;
using EventPlatform.Application.DTOs.Sessions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatform.Application.Interfaces
{
    public interface ISessionService
    {
        Task<PagedResultDto<SessionResponseDto>> GetByEventAsync(Guid eventId, int page, int pageSize, Guid? userId, string? status);
        Task<PagedResultDto<SessionResponseDto>> GetMySessionsAsync(int page, int pageSize, Guid? speakerId, string? status);
        Task<SessionResponseDto> GetByIdAsync(Guid id, Guid? userId);
        Task<SessionResponseDto> CreateAsync(CreateSessionDto dto);
        Task<SessionResponseDto> UpdateAsync(Guid id, UpdateSessionDto dto);
        Task<MessageResponseDto> UpdateStatusAsync(Guid id, string status);
        Task DeleteAsync(Guid id);
    }
}

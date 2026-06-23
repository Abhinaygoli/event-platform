using EventPlatform.Application.DTOs.Agenda;
using EventPlatform.Application.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatform.Application.Interfaces
{
    public interface IAgendaService
    {
        Task<List<AgendaItemResponseDto>> GetMyAgendaAsync(Guid userId);
        Task<MessageResponseDto> AddToAgendaAsync(Guid sessionId, Guid userId);
        Task<MessageResponseDto> RemoveFromAgendaAsync(Guid sessionId, Guid userId);
    }
}

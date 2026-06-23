using EventPlatform.Application.DTOs.Speakers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatform.Application.Interfaces
{
    public interface ISpeakerService
    {
        Task<List<SpeakerResponseDto>> GetAllAsync();
        Task<SpeakerResponseDto> GetByIdAsync(Guid id);
        Task<SpeakerResponseDto> GetByUserIdAsync(Guid userId);
        Task<SpeakerResponseDto> CreateAsync(CreateSpeakerDto dto, Guid userId);
        Task<SpeakerResponseDto> UpdateAsync(Guid userId, UpdateSpeakerDto dto);
    }
}

using EventPlatform.Application.DTOs.Common;
using EventPlatform.Application.DTOs.Favorites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatform.Application.Interfaces
{
    public interface IFavoriteService
    {
        Task<List<FavoriteResponseDto>> GetMyFavoritesAsync(Guid userId);
        Task<MessageResponseDto> AddFavoriteAsync(Guid sessionId, Guid userId);
        Task<MessageResponseDto> RemoveFavoriteAsync(Guid sessionId, Guid userId);
    }
}

using EventPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EventPlatform.API.Extensions;

namespace EventPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FavoritesController : ControllerBase
    {
        private readonly IFavoriteService _favoriteService;
        public FavoritesController(IFavoriteService favoriteService) => _favoriteService = favoriteService;

        /// <summary>Get my favorite sessions</summary>
        [HttpGet]
        public async Task<IActionResult> GetMyFavorites()
        {
            var userId = User.GetUserId();
            return Ok(await _favoriteService.GetMyFavoritesAsync(userId));
        }

        /// <summary>Add session to favorites</summary>
        [HttpPost("{sessionId:guid}")]
        public async Task<IActionResult> AddFavorite(Guid sessionId)
        {
            try
            {
                var userId = User.GetUserId();
                return Ok(await _favoriteService.AddFavoriteAsync(sessionId, userId));
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        /// <summary>Remove session from favorites</summary>
        [HttpDelete("{sessionId:guid}")]
        public async Task<IActionResult> RemoveFavorite(Guid sessionId)
        {
            try
            {
                var userId = User.GetUserId();
                return Ok(await _favoriteService.RemoveFavoriteAsync(sessionId, userId));
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }
    }
}

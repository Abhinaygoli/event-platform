using EventPlatform.API.Extensions;
using EventPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AgendaController : ControllerBase
    {
        private readonly IAgendaService _agendaService;
        public AgendaController(IAgendaService agendaService) => _agendaService = agendaService;

        /// <summary>Get my personal agenda (all authenticated roles)</summary>
        [HttpGet]
        public async Task<IActionResult> GetMyAgenda()
        {
            var userId = User.GetUserId();
            return Ok(await _agendaService.GetMyAgendaAsync(userId));
        }

        /// <summary>Add session to agenda</summary>
        [HttpPost("{sessionId:guid}")]
        public async Task<IActionResult> AddToAgenda(Guid sessionId)
        {
            try
            {
                var userId = User.GetUserId();
                return Ok(await _agendaService.AddToAgendaAsync(sessionId, userId));
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        /// <summary>Remove session from agenda</summary>
        [HttpDelete("{sessionId:guid}")]
        public async Task<IActionResult> RemoveFromAgenda(Guid sessionId)
        {
            try
            {
                var userId = User.GetUserId();
                return Ok(await _agendaService.RemoveFromAgendaAsync(sessionId, userId));
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }
    }
}

using EventPlatform.API.Extensions;
using EventPlatform.Application.DTOs.Sessions;
using EventPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionsController : ControllerBase
    {
        private readonly ISessionService _sessionService;
        public SessionsController(ISessionService sessionService) => _sessionService = sessionService;

        /// <summary>Get sessions for an event (public, paged)</summary>
        [HttpGet]
        public async Task<IActionResult> GetByEvent(
            [FromQuery] Guid eventId,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = User.GetUserIdOrNull();
            var result = await _sessionService.GetByEventAsync(eventId, page, pageSize, userId, status);
            return Ok(result);
        }

        [Authorize(Roles = "Speaker")]
        [HttpGet("my-sessions")]
        public async Task<IActionResult> GetMySessions(
            int page = 1,
            int pageSize = 20,
            string? status = null)
        {
            var speakerId =User.GetUserId();
            var result =
               await _sessionService.GetMySessionsAsync(
                   page,
                   pageSize,
                   speakerId,
                   status
               );


            return Ok(result);
        }

        /// <summary>Get session by ID (public)</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var userId = User.GetUserIdOrNull();
                return Ok(await _sessionService.GetByIdAsync(id, userId));
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        /// <summary>Create session (Admin only)</summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateSessionDto dto)
        {
            try
            {
                var result = await _sessionService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        /// <summary>Update session (Admin only)</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSessionDto dto)
        {
            try
            {
                return Ok(await _sessionService.UpdateAsync(id, dto));
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        /// <summary>Update session status (Admin only) — Scheduled|Live|Completed|Cancelled</summary>
        [HttpPatch("{id:guid}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateSessionStatusDto dto)
        {
            try
            {
                return Ok(await _sessionService.UpdateStatusAsync(id, dto.Status));
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        /// <summary>Delete session (Admin only)</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _sessionService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }
    }
}

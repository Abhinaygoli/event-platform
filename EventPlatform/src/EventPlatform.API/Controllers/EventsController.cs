using EventPlatform.API.Extensions;
using EventPlatform.Application.DTOs.Events;
using EventPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        public EventsController(IEventService eventService) => _eventService = eventService;

        /// <summary>Get all events (public, paged)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = User.GetUserIdOrNull();
            var result = await _eventService.GetAllAsync(page, pageSize, userId, status);
            return Ok(result);
        }

        /// <summary>Get all event summaries (public)</summary>
        [HttpGet("getallsummaries")]
        public async Task<IActionResult> GetAllSummariesAsync()
        {
            var result = await _eventService.GetAllSummariesAsync();
            return Ok(result);
        } 

        /// <summary>Get event by ID (public)</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var userId = User.GetUserIdOrNull();
                var result = await _eventService.GetByIdAsync(id, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        /// <summary>Create a new event (Admin only)</summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateEventDto dto)
        {
            try
            {
                var adminId = User.GetUserId();
                var result = await _eventService.CreateAsync(dto, adminId);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        /// <summary>Update event (Admin only)</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventDto dto)
        {
            try
            {
                var adminId = User.GetUserId();
                var result = await _eventService.UpdateAsync(id, dto, adminId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        /// <summary>Delete event (Admin only)</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _eventService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        /// <summary>Register current user for event (Attendee)</summary>
        [HttpPost("{id:guid}/register")]
        [Authorize(Roles = "Attendee")]
        public async Task<IActionResult> Register(Guid id)
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _eventService.RegisterAsync(id, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        /// <summary>Unregister current user from event (Attendee)</summary>
        [HttpDelete("{id:guid}/register")]
        [Authorize(Roles = "Attendee")]
        public async Task<IActionResult> Unregister(Guid id)
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _eventService.UnregisterAsync(id, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }
    }
}

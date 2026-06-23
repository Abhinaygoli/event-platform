using EventPlatform.API.Extensions;
using EventPlatform.Application.DTOs.Speakers;
using EventPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpeakersController : ControllerBase
    {
        private readonly ISpeakerService _speakerService;
        public SpeakersController(ISpeakerService speakerService) => _speakerService = speakerService;

        /// <summary>Get all speakers (public)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _speakerService.GetAllAsync());

        /// <summary>Get speaker by ID (public)</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try { return Ok(await _speakerService.GetByIdAsync(id)); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        /// <summary>Get my speaker profile (Speaker)</summary>
        [HttpGet("me")]
        [Authorize(Roles = "Speaker")]
        public async Task<IActionResult> GetMine()
        {
            try
            {
                var userId = User.GetUserId();
                return Ok(await _speakerService.GetByUserIdAsync(userId));
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        /// <summary>Create speaker profile (Speaker or Admin)</summary>
        [HttpPost]
        [Authorize(Roles = "Speaker,Admin")]
        public async Task<IActionResult> Create([FromBody] CreateSpeakerDto dto)
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _speakerService.CreateAsync(dto, userId);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        /// <summary>Update my speaker profile (Speaker or Admin)</summary>
        [HttpPut("me")]
        [Authorize(Roles = "Speaker,Admin")]
        public async Task<IActionResult> UpdateMine([FromBody] UpdateSpeakerDto dto)
        {
            try
            {
                var userId = User.GetUserId();
                return Ok(await _speakerService.UpdateAsync(userId, dto));
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }
    }
}

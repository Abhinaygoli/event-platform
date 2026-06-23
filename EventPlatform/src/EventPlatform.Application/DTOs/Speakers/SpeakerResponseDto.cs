using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatform.Application.DTOs.Speakers
{
    public class SpeakerResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? Company { get; set; }
        public string? Designation { get; set; }
        public string? PhotoUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public int SessionCount { get; set; }
    }
}

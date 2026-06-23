using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatform.Application.DTOs.Sessions
{
    public class UpdateSessionDto
    {
        public Guid? SpeakerId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Tags { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Room { get; set; }
        public int MaxCapacity { get; set; } = 100;
    }
}

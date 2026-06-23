using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatform.Application.DTOs.Favorites
{
    public class FavoriteResponseDto
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public string SessionTitle { get; set; } = string.Empty;
        public string EventTitle { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public string? SpeakerName { get; set; }
        public string? Tags { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

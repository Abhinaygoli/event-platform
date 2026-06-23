using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatform.Application.DTOs.Sessions
{
    public class SessionResponseDto
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public Guid? SpeakerId { get; set; }
        public string? SpeakerName { get; set; }
        public string? SpeakerCompany { get; set; }
        public string? SpeakerPhotoUrl { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Tags { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Room { get; set; }
        public string Status { get; set; } = string.Empty;
        public int MaxCapacity { get; set; }
        public int AgendaCount { get; set; }
        public int FavoriteCount { get; set; }
        public bool IsInAgenda { get; set; }
        public bool IsFavorited { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

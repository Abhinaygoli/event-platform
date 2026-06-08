using EventPlatform.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatform.Domain.Entities
{
    public class Session
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid EventId { get; set; }
        public Guid? SpeakerId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Tags { get; set; } // comma-separated: "dotnet,azure,api"
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Room { get; set; }
        public SessionStatus Status { get; set; } = SessionStatus.Scheduled;
        public int MaxCapacity { get; set; } = 100;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Event Event { get; set; } = null!;
        public Speaker? Speaker { get; set; }
        public ICollection<AgendaItem> AgendaItems { get; set; } = new List<AgendaItem>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    }
}

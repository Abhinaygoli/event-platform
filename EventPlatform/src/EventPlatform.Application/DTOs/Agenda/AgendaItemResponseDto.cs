using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatform.Application.DTOs.Agenda
{
    public class AgendaItemResponseDto
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public string SessionTitle { get; set; } = string.Empty;
        public string EventTitle { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Room { get; set; }
        public string? SpeakerName { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; }
    }
}

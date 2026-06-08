using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatform.Domain.Entities
{
    public class Speaker
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string? Bio { get; set; }
        public string? Company { get; set; }
        public string? Designation { get; set; }
        public string? PhotoUrl { get; set; }
        public string? LinkedInUrl { get; set; }

        // Navigation
        public User User { get; set; } = null!;
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
    }
}

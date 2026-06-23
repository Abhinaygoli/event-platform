using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatform.Application.DTOs.Sessions
{
    public class UpdateSessionStatusDto
    {
        /// <summary>
        /// Admin can only set: Cancelled | Scheduled (to un-cancel a session).
        /// Live and Completed are computed automatically from session times.
        /// </summary>
        public string Status { get; set; } = string.Empty; // Scheduled|Live|Completed|Cancelled
    }
}

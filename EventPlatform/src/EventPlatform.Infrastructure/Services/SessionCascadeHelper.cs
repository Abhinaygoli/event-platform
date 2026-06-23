using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatform.Infrastructure.Services
{
    /// <summary>
    /// Centralizes the soft-delete cascade logic for a Session.
    /// Used by EventService (cascading from event delete) and
    /// SessionService (direct session delete) so the cascade rule
    /// is defined ONCE and can never drift out of sync between them.
    /// </summary>
    public static class SessionCascadeHelper
    {
        public static void MarkSessionDeleted(Domain.Entities.Session session)
        {
            session.IsDeleted = true;

            foreach (var agendaItem in session.AgendaItems)
                agendaItem.IsDeleted = true;

            foreach (var favorite in session.Favorites)
                favorite.IsDeleted = true;
        }
    }
}

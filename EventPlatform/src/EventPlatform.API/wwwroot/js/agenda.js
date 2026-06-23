async function loadMyAgenda() {
    const container = document.getElementById('agenda-list');
    if (!container) return;

    container.innerHTML = `<div class="loading-spinner"></div>`;

    try {
        const items = await apiFetch('/api/agenda');
        if (!items.length) {
            container.innerHTML = `<div class="empty-state">
        <div class="empty-icon">📋</div>
        <h3>Your agenda is empty</h3>
        <p>Browse sessions and add them to your agenda.</p>
        <a href="events.html" class="btn btn-primary" style="margin-top:16px">Browse Events</a>
      </div>`;
            return;
        }

        // Group by date
        const grouped = items.reduce((acc, item) => {
            const date = new Date(item.startTime).toDateString();
            if (!acc[date]) acc[date] = [];
            acc[date].push(item);
            return acc;
        }, {});

        container.innerHTML = Object.entries(grouped).map(([date, sessions]) => `
      <div class="agenda-day">
        <div class="agenda-date-header">${date}</div>
        <div class="agenda-sessions">
          ${sessions.map(s => `
            <div class="agenda-item card" id="agenda-${s.id}">
              <div class="agenda-time">
                ${formatDateTime(s.startTime)} – ${formatDateTime(s.endTime)}
              </div>
              <div class="agenda-body">
                <div class="agenda-title">${s.sessionTitle}</div>
                <div class="agenda-meta">
                  ${s.eventTitle}
                  ${s.room ? ` · ${s.room}` : ''}
                  ${s.speakerName ? ` · 🎤 ${s.speakerName}` : ''}
                </div>
                <span class="session-status status-${s.status.toLowerCase()}">${s.status}</span>
              </div>
              <button
                class="btn btn-ghost btn-sm"
                onclick="removeFromAgenda('${s.sessionId}', '${s.id}')">
                Remove
              </button>
            </div>
          `).join('')}
        </div>
      </div>
    `).join('');
    } catch (err) {
        container.innerHTML = `<div class="error-state">${err.message}</div>`;
    }
}

async function removeFromAgenda(sessionId, itemId) {
    try {
        await apiFetch(`/api/agenda/${sessionId}`, { method: 'DELETE' });
        document.getElementById(`agenda-${itemId}`)?.remove();
        showToast('Removed from agenda.');
    } catch (err) { showToast(err.message, 'error'); }
}

async function loadMyFavorites() {
    const container = document.getElementById('favorites-list');
    if (!container) return;

    container.innerHTML = `<div class="loading-spinner"></div>`;

    try {
        const items = await apiFetch('/api/favorites');
        if (!items.length) {
            container.innerHTML = `<div class="empty-state">
        <div class="empty-icon">❤️</div>
        <h3>No favorites yet</h3>
        <p>Tap the heart on any session to save it here.</p>
      </div>`;
            return;
        }
        container.innerHTML = items.map(f => `
      <div class="favorite-card card" id="fav-${f.id}">
        <div class="fav-body">
          <div class="fav-title">${f.sessionTitle}</div>
          <div class="fav-meta">
            ${f.eventTitle}
            ${f.speakerName ? ` · 🎤 ${f.speakerName}` : ''}
            · ${formatDate(f.startTime)}
          </div>
          ${f.tags ? `<div class="session-tags">${f.tags.split(',').map(t => `<span class="tag-chip">${t.trim()}</span>`).join('')}</div>` : ''}
        </div>
        <button
          class="btn btn-ghost btn-sm"
          onclick="removeFavorite('${f.sessionId}', '${f.id}')">
          ❤️ Remove
        </button>
      </div>
    `).join('');
    } catch (err) {
        container.innerHTML = `<div class="error-state">${err.message}</div>`;
    }
}

async function removeFavorite(sessionId, itemId) {
    try {
        await apiFetch(`/api/favorites/${sessionId}`, { method: 'DELETE' });
        document.getElementById(`fav-${itemId}`)?.remove();
        showToast('Removed from favorites.');
    } catch (err) { showToast(err.message, 'error'); }
}

document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('agenda-list')) loadMyAgenda();
    if (document.getElementById('favorites-list')) loadMyFavorites();
});
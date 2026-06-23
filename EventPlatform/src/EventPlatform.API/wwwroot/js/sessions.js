let currentSessions = [];
// ── Load sessions for an event ────────────────────────────────
async function loadSessionsForEvent(eventId, page = 1, status) {
    const container = document.getElementsByClassName('sessions-list')[0];
    const container1 = document.getElementById('sessions-list');
    if (!container) return;

    container.innerHTML = `<div class="loading-spinner"></div>`;

    try {
        if (eventId == "") {
            container.innerHTML = '';
            return;
        }
        const data = await apiFetch(`/api/sessions?eventId=${eventId}&page=${page}&pageSize=20&status=${status}`);
        renderSessions(data.items, container);
        // currentSessions = data.items;
        // renderSessions(currentSessions,container,"live");
        renderPagination(data, 'sessions-pagination', (p) => loadSessionsForEvent(eventId, p, status));
    } catch (err) {
        container.innerHTML = `<div class="error-state">${err.message}</div>`;
    }
}

function renderSessions(sessions, container) {
    // const now = new Date();
    // const filteredSessions = sessions.filter(s => {
    //     const start = new Date(s.startTime);
    //     const end = new Date(s.endTime);
    //     switch (filter) {
    //         case "cancelled":
    //             return s.status === "Cancelled";
    //         case "live":
    //             return s.status === "Live";
    //         case "upcoming":
    //             return s.status === "Scheduled";
    //         case "past":
    //             return s.status === "Completed";
    //         default:
    //             return true;
    //     }

    //     });
    if (!sessions.length) {
        container.innerHTML = `<div class="empty-state">
      <div class="empty-icon">🎙</div>
      <h3>No sessions scheduled yet</h3>
    </div>`;
        return;
    }

    const role = Auth.getRole();
    container.innerHTML = sessions.map(s => `
    <div class="session-card card" id="session-${s.id}">
      <div class="session-header">
        <div class="session-time">
          <span>${formatDateTime(s.startTime)}</span>
          <span class="time-sep">→</span>
          <span>${formatDateTime(s.endTime)}</span>
        </div>
        <span class="session-status status-${s.status.toLowerCase()}">${s.status}</span>
      </div>
 
      <div class="session-body">
        <h3 class="session-title">${s.title}</h3>
        ${s.description ? `<p class="session-desc">${truncate(s.description, 120)}</p>` : ''}
 
        <div class="session-meta-row">
          ${s.speakerName ? `<span class="session-speaker">🎤 ${s.speakerName}${s.speakerCompany ? ` · ${s.speakerCompany}` : ''}</span>` : ''}
          ${s.room ? `<span class="session-room">🏛 ${s.room}</span>` : ''}
        </div>
 
        ${s.tags ? `
          <div class="session-tags">
            ${s.tags.split(',').map(t => `<span class="tag-chip">${t.trim()}</span>`).join('')}
          </div>` : ''}
      </div>
 
      <div class="session-footer">
        <div class="session-counts">
          <span>📋 ${s.agendaCount}</span>
          <span>❤️ ${s.favoriteCount}</span>
          <span>👥 ${s.maxCapacity} cap</span>
        </div>
 
        ${role === 'Admin' ? `
          <div class="session-actions">
          ${s.status === 'Completed' || s.status === 'Cancelled' ? ''
        : `<button class="btn btn-ghost btn-sm" onclick="openEditSession(${JSON.stringify(s).replace(/"/g, '&quot;')})">Edit</button>
            <button class="btn btn-ghost btn-sm" onclick="deleteSession('${s.id}')">Delete</button>`}
            ${s.status === 'Scheduled' ?
            `<select class="status-select" onchange="updateSessionStatus('${s.id}', this.value, '${s.eventId}')">
              <option value="">Change status</option>
              <option value="Scheduled">Scheduled</option>
              <option value="Cancelled">Cancel session</option>
            </select>` : ''}
          </div>` : ''}
 
        ${(role === 'Attendee' || role === 'Speaker') ? `
          <div class="session-actions">
            <button
              id="agenda-btn-${s.id}"
              class="btn btn-sm ${s.isInAgenda ? 'btn-primary' : 'btn-ghost'}"
              onclick="toggleAgenda('${s.id}', ${s.isInAgenda}, '${s.eventId}'))">
              ${s.isInAgenda ? '✓ In Agenda' : '+ Agenda'}
            </button>
            <button
              id="fav-btn-${s.id}"
              class="btn btn-sm btn-ghost"
              onclick="toggleFavorite('${s.id}', ${s.isFavorited})">
              ${s.isFavorited ? '❤️' : '🤍'}
            </button>
          </div>` : ''}
      </div>
    </div>
  `).join('');
}

document.querySelectorAll(".session-tab").forEach(tab => {
    tab.addEventListener("click", function () {
        document.querySelectorAll(".session-tab").forEach(t =>t.classList.remove("active"));
        this.classList.add("active");
        const eventId = new URLSearchParams(window.location.search).get('id');
        if (eventId) loadSessionsForEvent(eventId, 1, this.dataset.tab);
        // renderSessions(currentSessions, document.getElementsByClassName('sessions-list')[0], this.dataset.tab);
    });
});


// ── Admin: open edit session modal (pre-filled) ───────────────
function openEditSession(session) {
    // session is the full SessionResponseDto object passed from renderSessions
    // Fill in the edit form fields
    document.getElementById('edit-session-id').value = session.id;
    document.getElementById('edit-s-title').value = session.title || '';
    document.getElementById('edit-s-description').value = session.description || '';
    document.getElementById('edit-s-tags').value = session.tags || '';
    document.getElementById('edit-s-start').value = toLocalDatetimeInput(session.startTime);
    document.getElementById('edit-s-end').value = toLocalDatetimeInput(session.endTime);
    document.getElementById('edit-s-room').value = session.room || '';
    document.getElementById('edit-s-capacity').value = session.maxCapacity || 100;

    // Set speaker dropdown if it has a value
    const speakerSelect = document.getElementById('edit-s-speaker');
    if (speakerSelect && session.speakerId) {
        speakerSelect.value = session.speakerId;
    }

    openModal('edit-session-modal');
}

// ── Admin: submit edit session form ──────────────────────────
async function handleEditSession(e) {
    e.preventDefault();
    const btn = document.getElementById('edit-session-btn');
    btn.classList.add('btn-loading');
    btn.disabled = true;

    const id = document.getElementById('edit-session-id').value;

    try {
        const dto = {
            speakerId: document.getElementById('edit-s-speaker').value || null,
            title: document.getElementById('edit-s-title').value.trim(),
            description: document.getElementById('edit-s-description').value.trim(),
            tags: document.getElementById('edit-s-tags').value.trim(),
            startTime: convertLocalToUTC(document.getElementById('edit-s-start').value),
            endTime: convertLocalToUTC(document.getElementById('edit-s-end').value),
            room: document.getElementById('edit-s-room').value.trim(),
            maxCapacity: parseInt(document.getElementById('edit-s-capacity').value) || 100
        };

        let updatedSession = await apiFetch(`/api/sessions/${id}`, {
            method: 'PUT',
            body: JSON.stringify(dto)
        });

        showToast('Session updated!');
        closeModal('edit-session-modal');

        // Reload sessions for the current event
        const eventId = new URLSearchParams(window.location.search).get('id');
        //let activeTab = getActiveTab('session-tabs', 'session-tab')
        let activeTab = getActiveTabByDates(updatedSession.startTime, updatedSession.endTime)
        applyAdminActiveEventTab('session-tab', activeTab);
        if (eventId) loadSessionsForEvent(eventId, 1, activeTab);

    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        btn.classList.remove('btn-loading');
        btn.disabled = false;
    }
}

// ── Admin: create session ─────────────────────────────────────
async function handleCreateSession(e) {
    e.preventDefault();
    const btn = document.getElementById('create-session-btn');
    btn.classList.add('btn-loading'); btn.disabled = true;

    try {
        const eventId = new URLSearchParams(window.location.search).get('id');
        const dto = {
            eventId,
            speakerId: document.getElementById('s-speaker').value || null,
            title: document.getElementById('s-title').value.trim(),
            description: document.getElementById('s-description').value.trim(),
            tags: document.getElementById('s-tags').value.trim(),
            startTime: convertLocalToUTC(document.getElementById('s-start').value),
            endTime: convertLocalToUTC(document.getElementById('s-end').value),
            room: document.getElementById('s-room').value.trim(),
            maxCapacity: parseInt(document.getElementById('s-capacity').value) || 100
        };
        let createdSession = await apiFetch('/api/sessions', { method: 'POST', body: JSON.stringify(dto) });
        showToast('Session created!');
        closeModal('session-modal');
        // let activeTab = getActiveTab('session-tabs', 'session-tab')
        let activeTab = getActiveTabByDates(createdSession.startTime, createdSession.endTime)
        loadSessionsForEvent(eventId, 1, activeTab);
        applyAdminActiveEventTab('session-tab', activeTab);
    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        btn.classList.remove('btn-loading'); btn.disabled = false;
    }
}

// ── Admin: delete session ─────────────────────────────────────
async function deleteSession(id) {
    if (!confirm('Delete this session? This cannot be undone.')) return;
    try {
        await apiFetch(`/api/sessions/${id}`, { method: 'DELETE' });
        showToast('Session deleted.');
        document.getElementById(`session-${id}`)?.remove();
    } catch (err) { showToast(err.message, 'error'); }
}

// ── Admin: update session status ──────────────────────────────
// Only shown for Scheduled sessions — options: Scheduled / Cancelled
async function updateSessionStatus(id, status, eventId) {
    if (!status) return;
    try {
        var data = await apiFetch(`/api/sessions/${id}/status`, {
            method: 'PATCH',
            body: JSON.stringify({ status })
        });
        showToast(`Session marked as ${status}`);
        //const eventId = new URLSearchParams(window.location.search).get('id');
        //const eventId = data.eventId;
        let activeTab = getActiveTab('session-tabs', 'session-tab')
        if (eventId) loadSessionsForEvent(eventId, 1, activeTab);
    } catch (err) { showToast(err.message, 'error'); }
}

// ── Attendee: toggle agenda ───────────────────────────────────
async function toggleAgenda(sessionId, isInAgenda, eventId) {
    const btn = document.getElementById(`agenda-btn-${sessionId}`);
    if (btn) { btn.classList.add('btn-loading'); btn.disabled = true; }

    try {
        if (isInAgenda) {
            await apiFetch(`/api/agenda/${sessionId}`, { method: 'DELETE' });
            showToast('Removed from agenda.');
        } else {
            await apiFetch(`/api/agenda/${sessionId}`, { method: 'POST' });
            showToast('Added to agenda!');
        }
        // reload sessions to refresh button states
        //const eventId = new URLSearchParams(window.location.search).get('id');
        //const eventId = data.eventId;
        let activeTab = getActiveTab('session-tabs', 'session-tab')
        if (eventId) loadSessionsForEvent(eventId, 1, activeTab);
    } catch (err) {
        showToast(err.message, 'error');
        if (btn) { btn.classList.remove('btn-loading'); btn.disabled = false; }
    }
}

// ── Attendee: toggle favorite ─────────────────────────────────
async function toggleFavorite(sessionId, isFavorited) {
    const btn = document.getElementById(`fav-btn-${sessionId}`);
    if (btn) { btn.disabled = true; }

    try {
        if (isFavorited) {
            await apiFetch(`/api/favorites/${sessionId}`, { method: 'DELETE' });
            if (btn) btn.textContent = '🤍';
        } else {
            await apiFetch(`/api/favorites/${sessionId}`, { method: 'POST' });
            if (btn) btn.textContent = '❤️';
        }
        // toggle state
        btn && btn.setAttribute('onclick', `toggleFavorite('${sessionId}', ${!isFavorited})`);
    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        if (btn) btn.disabled = false;
    }
}

// ── Load all speakers for dropdowns ──────────────────────────
async function loadSpeakersDropdown() {
    const createSessionsSelect = document.getElementById('s-speaker');
    const editSessionSelect = document.getElementById('edit-s-speaker');
    if (!createSessionsSelect) return;
    if (!editSessionSelect) return;
    try {
        const speakers = await apiFetch('/api/speakers');
        createSessionsSelect.innerHTML = `<option value="">No speaker</option>` +
            speakers.map(sp => `<option value="${sp.id}">${sp.name}${sp.company ? ` (${sp.company})` : ''}</option>`).join('');

        editSessionSelect.innerHTML = `<option value="">No speaker</option>` +
            speakers.map(sp => `<option value="${sp.id}">${sp.name}${sp.company ? ` (${sp.company})` : ''}</option>`).join('');
    } catch { /* non-critical */ }
}

// ── Utility ───────────────────────────────────────────────────
// Converts ISO date string to value compatible with datetime-local input
function toLocalDatetimeInput(isoString) {
    if (!isoString) return '';
    const d = new Date(isoString);
    // Format: YYYY-MM-DDTHH:MM (no seconds, no timezone — browser handles local)
    return d.getFullYear() + '-' +
        String(d.getMonth() + 1).padStart(2, '0') + '-' +
        String(d.getDate()).padStart(2, '0') + 'T' +
        String(d.getHours()).padStart(2, '0') + ':' +
        String(d.getMinutes()).padStart(2, '0');
}

function convertLocalToUTC(dateTimeLocal) {
    if (!dateTimeLocal)
        return null;
    const localDate = new Date(dateTimeLocal);
    return localDate.toISOString();
}

function formatTime(dateStr) {
    return new Date(dateStr).toLocaleTimeString('en-IN', {
        hour: '2-digit', minute: '2-digit'
    });
}
function truncate(str, len) {
    return str.length > len ? str.slice(0, len) + '…' : str;
}

function openModal(id) { document.getElementById(id)?.classList.add('open'); }
function closeModal(id) { document.getElementById(id)?.classList.remove('open'); }

document.addEventListener('DOMContentLoaded', () => {
    const eventId = new URLSearchParams(window.location.search).get('id');
    if (eventId && document.getElementById('sessions-list')) {
        //loadSessionsForEvent(eventId, 1, "live");
        loadSpeakersDropdown();
        //loadSpeakersDropdown('edit-s-speaker'); // for edit modal too
    }
});
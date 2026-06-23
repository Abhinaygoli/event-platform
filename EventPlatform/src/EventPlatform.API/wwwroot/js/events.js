// ── Load and render all events ────────────────────────────────
async function loadEvents(page = 1, status ) {
    const grid = document.getElementById('events-grid');
    if (!grid) return;

    grid.innerHTML = `<div class="loading-spinner"></div>`;

    try {
        const data = await apiFetch(`/api/events?page=${page}&pageSize=9&status=${status}`);
        // let events = data.items;
        // if (state) {
        //     events = events.filter(ev => getEventCategory(ev) === state);
        // }
        // data.items = events;
        renderEvents(data.items, grid);
        renderPagination(data, 'events-pagination', () => loadEvents(page, status));
    } catch (err) {
        grid.innerHTML = `<div class="error-state">Failed to load events: ${err.message}</div>`;
    }
}

function renderEvents(events, container) {
    if (!events.length) {
        container.innerHTML = `<div class="empty-state">
      <div class="empty-icon">📅</div>
      <h3>No events yet</h3>
      <p>Check back soon for upcoming conferences.</p>
    </div>`;
        return;
    }
    const role = Auth.getRole();
    container.innerHTML = events.map(ev => `
    <div class="event-card card card-glow">
      ${ev.bannerUrl
            ? `<img src="${ev.bannerUrl}" alt="${ev.title}" class="event-banner"
             onclick="window.location.href='event-detail.html?id=${ev.id}'" style="cursor:pointer"/>`
            : `<div class="event-banner-placeholder"
             onclick="window.location.href='event-detail.html?id=${ev.id}'"
             style="cursor:pointer"></div>`}
      <div class="event-card-body">
        <div class="event-meta">
          <span class="badge badge-accent">${formatDate(ev.startDate)}</span>
          ${ev.isRegistered ? `<span class="badge badge-success">Registered ✓</span>` : ''}
        </div>
        <h3 class="event-title" onclick="window.location.href='event-detail.html?id=${ev.id}'"
          style="cursor:pointer">${ev.title}</h3>
        <p class="event-desc">
          ${ev.description ? truncate(ev.description, 100) : 'No description available.'}
        </p>
        <div class="event-footer">
          <div class="event-stats">
            <span>📋 ${ev.sessionCount} sessions</span>
            <span>👥 ${ev.registrationCount} registered</span>
          </div>
          ${ev.location ? `<span class="event-location">📍 ${ev.location}</span>` : ''}
        </div>
 
        ${role === 'Admin' ? `
          <div style="display:flex;gap:8px;margin-top:12px;flex-wrap:wrap">
            <a href="event-detail.html?id=${ev.id}" class="btn btn-ghost btn-sm">
              Manage Sessions
            </a>
            <button class="btn btn-ghost btn-sm"
              onclick="openEditEvent(${JSON.stringify(ev).replace(/"/g, '&quot;')})">
              Edit
            </button>
            <button class="btn btn-ghost btn-sm" style="color:var(--error)"
              onclick="deleteEvent('${ev.id}')">
              Delete
            </button>
          </div>` : ''}
 
        ${role === 'Attendee' ? `
          <div style="margin-top:12px">
            <button
              id="reg-btn-${ev.id}"
              class="btn btn-sm ${ev.isRegistered ? 'btn-ghost' : 'btn-primary'}"
              onclick="toggleEventRegistration('${ev.id}', ${ev.isRegistered})">
              ${ev.isRegistered ? 'Unregister' : 'Register'}
            </button>
          </div>` : ''}
      </div>
    </div>
  `).join('');
}

// ── Admin: open edit event modal (pre-filled) ─────────────────
function openEditEvent(event) {
    // event is the full EventResponseDto object
    document.getElementById('edit-ev-id').value = event.id;
    document.getElementById('edit-ev-title').value = event.title || '';
    document.getElementById('edit-ev-description').value = event.description || '';
    document.getElementById('edit-ev-location').value = event.location || '';
    document.getElementById('edit-ev-banner').value = event.bannerUrl || '';
    document.getElementById('edit-ev-start').value = toLocalDatetimeInput(event.startDate);
    document.getElementById('edit-ev-end').value = toLocalDatetimeInput(event.endDate);

    openModal('edit-event-modal');
}

// ── Admin: submit edit event form ─────────────────────────────
async function handleEditEvent(e) {
    e.preventDefault();
    const btn = document.getElementById('edit-event-btn');
    btn.classList.add('btn-loading');
    btn.disabled = true;

    const id = document.getElementById('edit-ev-id').value;

    try {
        const dto = {
            title: document.getElementById('edit-ev-title').value.trim(),
            description: document.getElementById('edit-ev-description').value.trim(),
            startDate: convertLocalToUTC(document.getElementById('edit-ev-start').value),
            endDate: convertLocalToUTC(document.getElementById('edit-ev-end').value),
            location: document.getElementById('edit-ev-location').value.trim(),
            bannerUrl: document.getElementById('edit-ev-banner').value.trim() || null
        };

        let event = await apiFetch(`/api/events/${id}`, {
            method: 'PUT',
            body: JSON.stringify(dto)
        });

        showToast('Event updated!');
        closeModal('edit-event-modal');
        const eventId = new URLSearchParams(window.location.search).get('id');
        if (eventId) {
            renderEventDetail(event);
        } else {
            // let activeTab = getActiveTab('admin-event-tabs', 'admin-event-tab')
            let activeTab = getActiveTabByDates(event.startDate, event.endDate)
            loadEvents(page = 1, activeTab); // reload the grid
            applyAdminActiveEventTab('admin-event-tab', activeTab);
        }

    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        btn.classList.remove('btn-loading');
        btn.disabled = false;
    }
}

// ── Admin: create event form ──────────────────────────────────
async function handleCreateEvent(e) {
    e.preventDefault();
    const btn = document.getElementById('create-event-btn');
    btn.classList.add('btn-loading');
    btn.disabled = true;

    try {
        const dto = {
            title: document.getElementById('ev-title').value.trim(),
            description: document.getElementById('ev-description').value.trim(),
            startDate: convertLocalToUTC(document.getElementById('ev-start').value),
            endDate: convertLocalToUTC(document.getElementById('ev-end').value),
            location: document.getElementById('ev-location').value.trim(),
            bannerUrl: document.getElementById('ev-banner').value.trim() || null
        };

        const result = await apiFetch('/api/events', {
            method: 'POST', body: JSON.stringify(dto)
        });
        showToast('Event created successfully!');
        document.getElementById('create-event-form').reset();
        //let activeTab = getActiveTab('admin-event-tabs', 'admin-event-tab')
        let activeTab = getActiveTabByDates(result.startDate, result.endDate)
        loadEvents(page = 1, activeTab); // reload the grid
        //admin-event-tab get item of this class name which contains text equals to active tab
        applyAdminActiveEventTab('admin-event-tab', activeTab)
        closeModal('event-modal');
    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        btn.classList.remove('btn-loading');
        btn.disabled = false;
    }
}

// ── Admin: delete event ───────────────────────────────────────
async function deleteEvent(id) {
    if (!confirm('Delete this event? All sessions and registrations will also be removed.')) return;
    try {
        await apiFetch(`/api/events/${id}`, { method: 'DELETE' });
        showToast('Event deleted.');
        let activeTab = getActiveTab('admin-event-tabs', 'admin-event-tab')
        loadEvents(page = 1, activeTab); // reload the grid
        applyAdminActiveEventTab('admin-event-tab', activeTab)
    } catch (err) { showToast(err.message, 'error'); }
}

// ── Attendee: register/unregister for event ───────────────────
async function toggleEventRegistration(eventId, isRegistered) {
    const btn = document.getElementById(`reg-btn-${eventId}`);
    if (btn) { btn.classList.add('btn-loading'); btn.disabled = true; }

    try {
        if (isRegistered) {
            await apiFetch(`/api/events/${eventId}/register`, { method: 'DELETE' });
            showToast('Unregistered from event.');
        } else {
            await apiFetch(`/api/events/${eventId}/register`, { method: 'POST' });
            showToast('Successfully registered!');
        }
        let activeTab = getActiveTab('admin-event-tabs', 'admin-event-tab')
        //let activeTad = getActiveTabByDates(event.startDate, event.endDate)
        loadEvents(page = 1, activeTab); // reload the grid
        //applyAdminActiveEventTab(activeTab)
        // loadEvents();
    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        if (btn) { btn.classList.remove('btn-loading'); btn.disabled = false; }
    }
}

// ── Load event detail page ────────────────────────────────────
function loadEventDetail() {
    const id = new URLSearchParams(window.location.search).get('id');
    if (!id) { window.location.href = 'events.html'; return; }

    try {
        loadEventDetails(id)
        loadSessionsForEvent(id, 1, "live");
    } catch (err) {
        showToast(err.message, 'error');
    }
}

async function loadEventDetails(id) {
    const ev = await apiFetch(`/api/events/${id}`);
    renderEventDetail(ev);
}

function renderEventDetail(ev) {
    const el = document.getElementById('event-detail');
    if (!el) return;
    const role = Auth.getRole();
    if (role === 'Admin' && canRegisterForEvent(ev)) {
        document.getElementById('admin-session-btn').classList.remove('hidden');
    }
    el.innerHTML = `
    <div class="event-detail-header">
      ${ev.bannerUrl
            ? `<img src="${ev.bannerUrl}" class="detail-banner"
             style="width:100%;max-height:300px;object-fit:cover;border-radius:16px;margin-bottom:24px"/>`
            : ''}
       <div style="display:flex;gap:8px;flex-wrap:wrap;margin-bottom:12px">
        <span class="badge badge-accent">${formatDate(ev.startDate)} → ${formatDate(ev.endDate)}</span>
        ${ev.location ? `<span class="badge badge-purple">📍 ${ev.location}</span>` : ''}
      </div>
      <h1 style="font-family:var(--font-display);font-size:32px;font-weight:800;
                 letter-spacing:-1px;margin-bottom:12px">${ev.title}</h1>
      <p style="font-size:16px;color:var(--text-secondary);line-height:1.7;
                max-width:700px;margin-bottom:20px">${ev.description || ''}</p>
      <div style="display:flex;gap:12px;flex-wrap:wrap;margin-bottom:24px">
        <div class="stat-chip"
          style="background:var(--bg-card);border:1px solid var(--border);
                 border-radius:8px;padding:8px 16px;font-size:13px">
          📋 ${ev.sessionCount} Sessions
        </div>
        <div class="stat-chip"
          style="background:var(--bg-card);border:1px solid var(--border);
                 border-radius:8px;padding:8px 16px;font-size:13px">
          👥 ${ev.registrationCount} Attendees
        </div>
      </div>
       <div style="display:flex;gap:10px;flex-wrap:wrap">
        ${(role === 'Attendee' && canRegisterForEvent(ev)) ? `
        <button
          id="reg-btn-${ev.id}"
          class="btn ${ev.isRegistered ? 'btn-ghost' : 'btn-primary'} register-button"
          onclick="toggleEventRegistration('${ev.id}', ${ev.isRegistered})">
          ${ev.isRegistered ? 'Unregister' : 'Register for Event'}
        </button>` : !canRegisterForEvent(ev)? `<span class="event-completed-badge">✓ Event Completed</span>` : ''}
 
        ${(role === 'Admin' && canRegisterForEvent(ev)) ? `
          <button class="btn btn-ghost"
            onclick="openEditEvent(${JSON.stringify(ev).replace(/"/g, '&quot;')})">
            Edit Event
          </button>` : ''}
      </div>
    </div>`;
}

// ── Pagination renderer ───────────────────────────────────────
function renderPagination(data, containerId, loadFn) {
    const container = document.getElementById(containerId);
    if (!container || data.totalPages <= 1) { if (container) container.innerHTML = ''; return; }

    container.innerHTML = `
    <div class="pagination">
      <button class="btn btn-ghost btn-sm" ${!data.hasPrev ? 'disabled' : ''}
        onclick="${loadFn.name}(${data.page - 1})">← Prev</button>
      <span class="page-info">Page ${data.page} of ${data.totalPages}</span>
      <button class="btn btn-ghost btn-sm" ${!data.hasNext ? 'disabled' : ''}
        onclick="${loadFn.name}(${data.page + 1})">Next →</button>
    </div>`;

    // container.innerHTML = `
    // <div class="pagination">
    //   <button class="btn btn-ghost btn-sm" ${!data.hasPrev ? 'disabled' : ''}
    //     onclick="(${loadFn.toString()})(${data.page - 1})">← Prev</button>
    //   <span class="page-info">Page ${data.page} of ${data.totalPages}</span>
    //   <button class="btn btn-ghost btn-sm" ${!data.hasNext ? 'disabled' : ''}
    //     onclick="(${loadFn.toString()})(${data.page + 1})">Next →</button>
    // </div>`;
}

// ── Utility helpers ───────────────────────────────────────────
function formatDate(dateStr) {
    return new Date(dateStr).toLocaleDateString('en-IN', {
        day: 'numeric', month: 'short', year: 'numeric'
    });
}

function formatDateTime(dateStr) {
    return new Date(dateStr).toLocaleString('en-IN', {
        day: '2-digit',
        month: 'short',
        hour: '2-digit',
        minute: '2-digit'
    });
}

// ── Utilities ─────────────────────────────────────────────────
function toLocalDatetimeInput(isoString) {
    if (!isoString) return '';
    const d = new Date(isoString);
    return d.getFullYear() + '-' +
        String(d.getMonth() + 1).padStart(2, '0') + '-' +
        String(d.getDate()).padStart(2, '0') + 'T' +
        String(d.getHours()).padStart(2, '0') + ':' +
        String(d.getMinutes()).padStart(2, '0');
}
function truncate(str, len) {
    return str.length > len ? str.slice(0, len) + '…' : str;
}
function openModal(id) { document.getElementById(id)?.classList.add('open'); }
function closeModal(id) { document.getElementById(id)?.classList.remove('open'); }

function getEventCategory(event) {
    const today = new Date();
    const start = new Date(event.startDate);
    const end = new Date(event.endDate);
    if (today >= start && today <= end) {
        return "live";
    }
    if (today < start) {
        return "upcoming";
    }
    return "past";
}

function canRegisterForEvent(ev) {
    const now = new Date();
    const start = new Date(ev.startDate);
    const end = new Date(ev.endDate);
    // already completed
    if (now > end)
        return false;
    return true;
}

function getActiveTab(containerClass, tabClass) {
    const activeTab = document.querySelector(
        `.${containerClass} .${tabClass}.active`
    );
    return activeTab?.dataset.tab ?? null;
}
function getActiveTabByDates(startTime, endTime) {
    const now = new Date();
    const start = new Date(startTime);
    const end = new Date(endTime);
    //Here write a code we need to get the status based on dates.
    if (now >= start & now <= end) {
        return "live"
    } else if (now <= start) {
        return "upcoming"
    } else if (now >= start & now >= end) {
        return "past"
    }
}

function applyAdminActiveEventTab(page, activeTab) {
    if (page == 'admin-event-tab') {
        document.querySelectorAll('.admin-event-tab').forEach(tab => {
            tab.textContent.toLowerCase() === activeTab ? tab.classList.add('active') : tab.classList.remove('active')
        });
    } else if (page == 'session-tab') {
        document.querySelectorAll('.session-tab').forEach(tab => {
            tab.textContent.toLowerCase() === activeTab ? tab.classList.add('active') : tab.classList.remove('active')
        });
    }
}

document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('events-grid')) loadEvents(page = 1, status = "upcoming");
    if (document.getElementById('event-detail')) loadEventDetail();
});
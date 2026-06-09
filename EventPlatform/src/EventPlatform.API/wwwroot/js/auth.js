// =============================================================
// wwwroot/js/auth.js
// Handles: JWT storage, login, register, logout, role redirect
// Uses: fetch API (modern, no jQuery dependency needed)
// =============================================================

const API_BASE = '';  // empty = same origin (your Render URL)

// ── Token helpers ─────────────────────────────────────────────
const Auth = {
    setTokens(data) {
        localStorage.setItem('ep_access_token', data.accessToken);
        //localStorage.setItem('ep_refresh_token', data.refreshToken);
        localStorage.setItem('ep_user_name', data.name);
        localStorage.setItem('ep_user_email', data.email);
        localStorage.setItem('ep_user_role', data.role);
        localStorage.setItem('ep_expires_at', data.expiresAt);
    },
    getToken() { return localStorage.getItem('ep_access_token'); },
    getRole() { return localStorage.getItem('ep_user_role'); },
    getName() { return localStorage.getItem('ep_user_name'); },
    getEmail() { return localStorage.getItem('ep_user_email'); },
    isLoggedIn() { return !!localStorage.getItem('ep_access_token'); },
    clear() {
        ['ep_access_token', 'ep_refresh_token', 'ep_user_name',
            'ep_user_email', 'ep_user_role', 'ep_expires_at'].forEach(k => localStorage.removeItem(k));
    },
    redirectByRole() {
        const role = Auth.getRole();
        if (role === 'Admin') { window.location.href = '/dashboard-admin.html'; return; }
        if (role === 'Speaker') { window.location.href = '/dashboard-speaker.html'; return; }
        if (role === 'Attendee') { window.location.href = '/dashboard-attendee.html'; return; }
        window.location.href = '/';
    },
    requireAuth() {
        if (!Auth.isLoggedIn()) { window.location.href = '/login.html'; }
    },
    logout() {
        Auth.clear();
        window.location.href = '/login.html';
    }
};

// ── API fetch wrapper ─────────────────────────────────────────
async function apiFetch(endpoint, options = {}) {
    const headers = { 'Content-Type': 'application/json', ...options.headers };
    const token = Auth.getToken();
    if (token) headers['Authorization'] = `Bearer ${token}`;

    const res = await fetch(`${API_BASE}${endpoint}`, { ...options, headers });
    const data = await res.json().catch(() => ({}));

    if (!res.ok) {
        const msg = data.message || data.title || `Error ${res.status}`;
        throw new Error(msg);
    }
    return data;
}

// ── Toast notifications ───────────────────────────────────────
function showToast(message, type = 'success') {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        document.body.appendChild(container);
    }
    const icons = { success: '✓', error: '✕', warning: '⚠' };
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.innerHTML = `
    <span class="toast-icon">${icons[type] || icons.success}</span>
    <span>${message}</span>
    <span class="toast-close" onclick="this.parentElement.remove()">×</span>`;
    container.appendChild(toast);
    setTimeout(() => toast.remove(), 4000);
}

// ── Password visibility toggle ────────────────────────────────
function togglePassword(toggleEl) {
    const input = toggleEl.previousElementSibling;
    const isText = input.type === 'text';
    input.type = isText ? 'password' : 'text';
    toggleEl.textContent = isText ? '👁' : '🙈';
}

// ── Register form handler ─────────────────────────────────────
async function handleRegister(e) {
    e.preventDefault();
    const btn = document.getElementById('register-btn');
    btn.classList.add('btn-loading');
    btn.disabled = true;

    const name = document.getElementById('name').value.trim();
    const email = document.getElementById('email').value.trim();
    const password = document.getElementById('password').value;
    const role = document.getElementById('role').value;

    // Basic validation
    clearErrors();
    let valid = true;
    if (!name) { showError('name-error', 'Name is required'); valid = false; }
    if (!email) { showError('email-error', 'Email is required'); valid = false; }
    if (password.length < 6) { showError('password-error', 'Password must be at least 6 characters'); valid = false; }
    if (!valid) { btn.classList.remove('btn-loading'); btn.disabled = false; return; }

    try {
        const data = await apiFetch('/api/auth/register', {
            method: 'POST',
            body: JSON.stringify({ name, email, password, role })
        });
        Auth.setTokens(data);
        showToast(`Welcome, ${data.name}! Redirecting...`);
        setTimeout(() => Auth.redirectByRole(), 1200);
    } catch (err) {
        showToast(err.message, 'error');
        btn.classList.remove('btn-loading');
        btn.disabled = false;
    }
}

// ── Login form handler ────────────────────────────────────────
async function handleLogin(e) {
    e.preventDefault();
    const btn = document.getElementById('login-btn');
    btn.classList.add('btn-loading');
    btn.disabled = true;

    const email = document.getElementById('email').value.trim();
    const password = document.getElementById('password').value;

    clearErrors();
    let valid = true;
    if (!email) { showError('email-error', 'Email is required'); valid = false; }
    if (!password) { showError('password-error', 'Password is required'); valid = false; }
    if (!valid) { btn.classList.remove('btn-loading'); btn.disabled = false; return; }

    try {
        const data = await apiFetch('/api/auth/login', {
            method: 'POST',
            body: JSON.stringify({ email, password })
        });
        Auth.setTokens(data);
        showToast(`Welcome back, ${data.name}!`);
        setTimeout(() => Auth.redirectByRole(), 1200);
    } catch (err) {
        showToast(err.message, 'error');
        btn.classList.remove('btn-loading');
        btn.disabled = false;
    }
}

// ── Error helpers ─────────────────────────────────────────────
function showError(id, msg) {
    const el = document.getElementById(id);
    if (el) { el.textContent = msg; el.classList.add('show'); }
}
function clearErrors() {
    document.querySelectorAll('.form-error').forEach(el => {
        el.textContent = '';
        el.classList.remove('show');
    });
}

// ── Health check ──────────────────────────────────────────────
async function checkHealth() {
    const el = document.getElementById('health-status');
    if (!el) return;
    try {
        const data = await apiFetch('/health');
        el.textContent = data.status + ' · ' + new Date(data.timestamp).toLocaleTimeString();
        el.style.color = 'var(--success)';
    } catch {
        el.textContent = 'unavailable';
        el.style.color = 'var(--error)';
    }
}

// Auto-run health check if element exists
document.addEventListener('DOMContentLoaded', checkHealth);
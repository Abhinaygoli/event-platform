// =============================================================
// wwwroot/js/auth.js
// Handles: JWT storage, login, register, logout, role redirect
// Uses: fetch API (modern, no jQuery dependency needed)
// =============================================================

const API_BASE = '';  // empty = same origin (your Render URL)

// ── Token helpers ─────────────────────────────────────────────
const Auth = {
    setTokens(data) {
        // Only store access token and user info — NOT refresh token
        // Refresh token lives in HttpOnly cookie (server sets it)
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
    clearLocal() {
        ['ep_access_token', 'ep_user_name',
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
    async logout() {
        try {
            // Call server logout first — revokes refresh token in DB
            // and tells browser to delete the HttpOnly cookie
            await fetch(`${API_BASE}/api/auth/logout`, {
                method: 'POST',
                credentials: 'include', // sends the HttpOnly cookie so server can revoke it
                headers: { 'Content-Type': 'application/json' }
            });
        } catch {
            // Even if server call fails, clear local state and redirect
        } finally {
            Auth.clearLocal();
            window.location.href = '/login.html';
        }
    }
};

// ── Check if access token is expired ─────────────────────────
// Reads the expiry from localStorage and compares to now.
// Returns true if expired or missing.
function isTokenExpired() {
    const expiresAt = localStorage.getItem('ep_expires_at');
    if (!expiresAt) return true;
    // Add 10 second buffer — refresh before it actually expires
    return new Date(expiresAt) <= new Date(Date.now() + 10_000);
}

// ── Silent refresh on page load ───────────────────────────────
// Call this at the top of EVERY dashboard page before rendering.
// If access token is expired → silently calls /api/auth/refresh
// using the HttpOnly cookie → gets new access token → continues.
// If refresh also fails → user is not logged in → redirect to login.
async function requireAuthWithRefresh() {
    // Step 1: not logged in at all → go to login
    if (!Auth.isLoggedIn()) {
        window.location.href = '/login.html';
        return false;
    }

    // Step 2: token still valid → nothing to do
    if (!isTokenExpired()) {
        return true;
    }

    // Step 3: token expired → try silent refresh via HttpOnly cookie
    try {
        const res = await fetch('/api/auth/refresh', {
            method: 'POST',
            credentials: 'include', // sends HttpOnly cookie automatically
            headers: { 'Content-Type': 'application/json' }
        });

        if (!res.ok) throw new Error('Refresh failed');

        const data = await res.json();
        Auth.setTokens(data); // save new access token
        return true;          // continue loading dashboard

    } catch {
        // Refresh failed — session fully expired
        Auth.clearLocal();
        window.location.href = '/login.html';
        return false;
    }
}

// ── Refresh token interceptor ─────────────────────────────────
// Flag prevents multiple simultaneous refresh calls
let _isRefreshing = false;
let _refreshQueue = []; // queued requests waiting for new token

function _processQueue(error, token = null) {
    _refreshQueue.forEach(({ resolve, reject }) => {
        if (error) reject(error);
        else resolve(token);
    });
    _refreshQueue = [];
}

async function tryRefreshToken() {
    // No body needed — browser sends the HttpOnly cookie automatically
    // credentials: 'include' is required for cross-origin cookie sending
    const res = await fetch(`${API_BASE}/api/auth/refresh`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' }
        // No body — server reads cookie from Request.Cookies["ep_refresh_token"]
    });

    if (!res.ok) {
        throw new Error('Session expired. Please log in again.');
    }

    const data = await res.json();
    Auth.setTokens(data); // saves new access token + user info
    return data.accessToken;
}

// ── Core API fetch with 401 auto-refresh ──────────────────────
async function apiFetch(endpoint, options = {}) {
    const makeRequest = async (token) => {
        const headers = { 'Content-Type': 'application/json', ...options.headers };
        if (token) headers['Authorization'] = `Bearer ${token}`;
        return fetch(`${API_BASE}${endpoint}`, {
            ...options,
            headers,
            credentials: 'include' // always include cookies for /api/auth/* routes
        });
    };

    // First attempt with current access token
    let res = await makeRequest(Auth.getToken());

    // ── 401 interceptor ──────────────────────────────────────
    if (res.status === 401) {

        // If already refreshing, queue this request until done
        if (_isRefreshing) {
            return new Promise((resolve, reject) => {
                _refreshQueue.push({ resolve, reject });
            })
                .then(newToken => makeRequest(newToken))
                .then(async r => {
                    const d = await r.json().catch(() => ({}));
                    if (!r.ok) throw new Error(d.message || `Error ${r.status}`);
                    return d;
                });
        }

        _isRefreshing = true;

        try {
            const newToken = await tryRefreshToken();
            _processQueue(null, newToken); // unblock queued requests

            // Retry original request with fresh access token
            const retryRes = await makeRequest(newToken);
            const retryData = await retryRes.json().catch(() => ({}));

            if (!retryRes.ok) {
                throw new Error(retryData.message || retryData.title || `Error ${retryRes.status}`);
            }
            return retryData;

        } catch (refreshErr) {
            _processQueue(refreshErr, null);
            // Refresh failed — session truly expired — force logout
            showToast('Your session has expired. Please log in again.', 'error');
            setTimeout(() => Auth.logout(), 1500);
            throw refreshErr;

        } finally {
            _isRefreshing = false;
        }
    }
    // ── End 401 interceptor ───────────────────────────────────

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
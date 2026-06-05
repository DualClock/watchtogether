// ===== API Client =====
const API_BASE_URL = window.location.hostname === 'localhost' 
    ? 'http://localhost:5000' 
    : ''; // Use relative path for production (same origin or proxy)

class WatchTogetherAPI {
    constructor() {
        this.token = localStorage.getItem('accessToken');
        this.refreshToken = localStorage.getItem('refreshToken');
    }

    setTokens(accessToken, refreshToken) {
        this.token = accessToken;
        this.refreshToken = refreshToken;
        localStorage.setItem('accessToken', accessToken);
        localStorage.setItem('refreshToken', refreshToken);
    }

    clearTokens() {
        this.token = null;
        this.refreshToken = null;
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
    }

    isAuthenticated() {
        return !!this.token;
    }

    async request(endpoint, options = {}) {
        const url = `${API_BASE_URL}${endpoint}`;
        const headers = {
            'Content-Type': 'application/json',
            ...options.headers
        };

        if (this.token) {
            headers['Authorization'] = `Bearer ${this.token}`;
        }

        try {
            const response = await fetch(url, {
                ...options,
                headers
            });

            if (response.status === 401) {
                // Try to refresh token
                const refreshed = await this.refreshAccessToken();
                if (refreshed) {
                    headers['Authorization'] = `Bearer ${this.token}`;
                    const retryResponse = await fetch(url, {
                        ...options,
                        headers
                    });
                    return this.handleResponse(retryResponse);
                }
                this.clearTokens();
                window.location.href = '/pages/login.html';
                return null;
            }

            return this.handleResponse(response);
        } catch (error) {
            showToast('Network error. Please try again.', 'error');
            throw error;
        }
    }

    async handleResponse(response) {
        if (!response.ok) {
            const error = await response.json().catch(() => ({ error: 'Unknown error' }));
            throw new Error(error.error || `HTTP ${response.status}`);
        }
        return response.status === 204 ? null : response.json();
    }

    async refreshAccessToken() {
        if (!this.refreshToken) return false;

        try {
            const response = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ refreshToken: this.refreshToken })
            });

            if (response.ok) {
                const data = await response.json();
                this.setTokens(data.accessToken, data.refreshToken);
                return true;
            }
        } catch (e) {
            console.error('Token refresh failed:', e);
        }
        return false;
    }

    // Auth
    async register(data) {
        const result = await this.request('/api/auth/register', {
            method: 'POST',
            body: JSON.stringify(data)
        });
        if (result) {
            this.setTokens(result.accessToken, result.refreshToken);
        }
        return result;
    }

    async login(data) {
        const result = await this.request('/api/auth/login', {
            method: 'POST',
            body: JSON.stringify(data)
        });
        if (result) {
            this.setTokens(result.accessToken, result.refreshToken);
        }
        return result;
    }

    async logout() {
        await this.request('/api/auth/logout', { method: 'POST' });
        this.clearTokens();
    }

    // Users
    async getProfile() {
        return this.request('/api/users/me');
    }

    async updateProfile(data) {
        return this.request('/api/users/me', {
            method: 'PUT',
            body: JSON.stringify(data)
        });
    }

    async uploadAvatar(file) {
        const formData = new FormData();
        formData.append('file', file);

        const response = await fetch(`${API_BASE_URL}/api/users/me/avatar`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${this.token}`
            },
            body: formData
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Upload failed');
        }
        return response.json();
    }

    async getUserByUsername(username) {
        return this.request(`/api/users/${username}`);
    }

    async deleteAccount() {
        return this.request('/api/users/me', { method: 'DELETE' });
    }

    // Rooms
    async getRooms(page = 1, pageSize = 20) {
        return this.request(`/api/rooms?page=${page}&pageSize=${pageSize}`);
    }

    async searchRooms(query, page = 1, pageSize = 20) {
        return this.request(`/api/rooms/search?query=${encodeURIComponent(query)}&page=${page}&pageSize=${pageSize}`);
    }

    async getRoom(id) {
        return this.request(`/api/rooms/${id}`);
    }

    async createRoom(data) {
        return this.request('/api/rooms', {
            method: 'POST',
            body: JSON.stringify(data)
        });
    }

    async joinRoom(id, password = null) {
        return this.request(`/api/rooms/${id}/join`, {
            method: 'POST',
            body: JSON.stringify({ password })
        });
    }

    async leaveRoom(id) {
        return this.request(`/api/rooms/${id}/leave`, { method: 'POST' });
    }

    async closeRoom(id) {
        return this.request(`/api/rooms/${id}`, { method: 'DELETE' });
    }

    // Messages
    async getMessages(roomId, page = 1, pageSize = 50) {
        return this.request(`/api/rooms/${roomId}/messages?page=${page}&pageSize=${pageSize}`);
    }

    async sendMessage(roomId, content, parentMessageId = null) {
        return this.request(`/api/rooms/${roomId}/messages`, {
            method: 'POST',
            body: JSON.stringify({ content, parentMessageId })
        });
    }
}

// Global API instance
const api = new WatchTogetherAPI();

// ===== Toast Notifications =====
function showToast(message, type = 'success') {
    let container = document.querySelector('.toast-container');
    if (!container) {
        container = document.createElement('div');
        container.className = 'toast-container';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.textContent = message;
    container.appendChild(toast);

    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateX(100%)';
        setTimeout(() => toast.remove(), 300);
    }, 4000);
}

// ===== Auth Guard =====
function requireAuth() {
    if (!api.isAuthenticated()) {
        window.location.href = '/pages/login.html';
        return false;
    }
    return true;
}

function redirectIfAuth() {
    if (api.isAuthenticated()) {
        window.location.href = '/pages/rooms.html';
        return true;
    }
    return false;
}

// ===== Navbar =====
async function renderNavbar() {
    const navbar = document.querySelector('.navbar');
    if (!navbar) return;

    const isAuth = api.isAuthenticated();
    let user = null;

    if (isAuth) {
        try {
            user = await api.getProfile();
        } catch (e) {
            console.error('Failed to load profile:', e);
        }
    }

    const userInitial = user?.displayName?.[0] || user?.username?.[0] || '?';
    const displayName = user?.displayName || user?.username || 'User';

    // Keep the logo, replace nav links and user menu
    const logo = navbar.querySelector('.logo');
    const existingNav = navbar.querySelector('nav');
    const existingUserMenu = navbar.querySelector('.user-menu') || navbar.querySelector('div:last-child');
    
    if (existingNav) existingNav.remove();
    if (existingUserMenu && existingUserMenu !== logo?.parentElement) existingUserMenu.remove();

    const nav = document.createElement('nav');
    nav.innerHTML = `
        <ul class="nav-links">
            <li><a href="/pages/rooms.html" class="${location.pathname.includes('rooms') ? 'active' : ''}">Комнаты</a></li>
            <li><a href="/pages/index.html#features" class="${location.pathname.includes('index') ? 'active' : ''}">Возможности</a></li>
            ${isAuth ? `
                <li><a href="/pages/profile.html" class="${location.pathname.includes('profile') ? 'active' : ''}">Профиль</a></li>
            ` : ''}
        </ul>
    `;
    
    const userMenu = document.createElement('div');
    userMenu.className = 'user-menu';
    userMenu.style.cssText = 'display:flex;align-items:center;gap:16px;';
    userMenu.innerHTML = isAuth ? `
        <span style="color: var(--text-secondary);font-size:0.9rem;font-weight:500;">${displayName}</span>
        <div class="user-avatar" onclick="window.location.href='/pages/profile.html'" style="width:36px;height:36px;border-radius:50%;background:var(--accent);display:flex;align-items:center;justify-content:center;color:white;font-weight:700;cursor:pointer;font-size:0.875rem;">
            ${user?.avatarUrl ? `<img src="${API_BASE_URL}${user.avatarUrl}" style="width:100%;height:100%;border-radius:50%;object-fit:cover;">` : userInitial}
        </div>
        <button class="btn btn-secondary" onclick="handleLogout()" style="padding:8px 20px;font-size:0.85rem;">Выйти</button>
    ` : `
        <a href="/pages/login.html" class="btn btn-secondary" style="padding:8px 20px;font-size:0.85rem;">Войти</a>
        <a href="/pages/register.html" class="btn btn-primary" style="padding:8px 20px;font-size:0.85rem;">Регистрация</a>
    `;

    navbar.appendChild(nav);
    navbar.appendChild(userMenu);
}

async function handleLogout() {
    try {
        await api.logout();
    } catch (e) {
        console.error('Logout error:', e);
    }
    api.clearTokens();
    window.location.href = '/pages/login.html';
}

// ===== Form Validation =====
function validateEmail(email) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

function validatePassword(password) {
    return password.length >= 8 &&
        /[A-Z]/.test(password) &&
        /[a-z]/.test(password) &&
        /[0-9]/.test(password) &&
        /[^a-zA-Z0-9]/.test(password);
}

function validateUsername(username) {
    return /^[a-zA-Z0-9_]{3,30}$/.test(username);
}

// ===== Date Formatting =====
function formatTime(date) {
    const d = new Date(date);
    return d.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });
}

function formatDate(date) {
    const d = new Date(date);
    return d.toLocaleDateString('ru-RU', { day: 'numeric', month: 'short', year: 'numeric' });
}

function formatDateTime(date) {
    const d = new Date(date);
    const now = new Date();
    const diff = now - d;
    
    if (diff < 60000) return 'только что';
    if (diff < 3600000) return `${Math.floor(diff / 60000)} мин. назад`;
    if (diff < 86400000) return `${Math.floor(diff / 3600000)} ч. назад`;
    if (diff < 604800000) return `${Math.floor(diff / 86400000)} д. назад`;
    
    return formatDate(date);
}

// ===== Initialize =====
document.addEventListener('DOMContentLoaded', () => {
    renderNavbar();
});

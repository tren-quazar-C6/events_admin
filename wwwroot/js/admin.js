// =============================================
// TELÓN ADMIN — admin.js
// =============================================

// ── Sidebar state ──────────────────────────
const SIDEBAR_KEY = 'telon_sidebar_open';

function isMobile() {
    return window.innerWidth < 768; // md breakpoint
}

function openSidebar() {
    const sidebar  = document.getElementById('sidebar');
    const overlay  = document.getElementById('sidebar-overlay');
    const icon     = document.getElementById('toggle-icon');

    sidebar.classList.remove('-translate-x-full');
    sidebar.classList.add('translate-x-0');

    if (isMobile()) {
        overlay.classList.remove('hidden');
        requestAnimationFrame(() => overlay.classList.replace('opacity-0', 'opacity-100'));
    }

    icon.classList.replace('fa-bars', 'fa-bars-staggered');
    if (!isMobile()) localStorage.setItem(SIDEBAR_KEY, 'open');
}

function closeSidebar() {
    const sidebar  = document.getElementById('sidebar');
    const overlay  = document.getElementById('sidebar-overlay');
    const icon     = document.getElementById('toggle-icon');

    sidebar.classList.add('-translate-x-full');
    sidebar.classList.remove('translate-x-0');

    overlay.classList.replace('opacity-100', 'opacity-0');
    setTimeout(() => overlay.classList.add('hidden'), 300);

    icon.classList.replace('fa-bars-staggered', 'fa-bars');
    if (!isMobile()) localStorage.setItem(SIDEBAR_KEY, 'closed');
}

function toggleSidebar() {
    const sidebar = document.getElementById('sidebar');
    const isOpen  = !sidebar.classList.contains('-translate-x-full');
    isOpen ? closeSidebar() : openSidebar();
}

// ── Init on load ───────────────────────────
document.addEventListener('DOMContentLoaded', () => {

    // Desktop: restore last state (default open)
    if (!isMobile()) {
        const saved = localStorage.getItem(SIDEBAR_KEY);
        if (saved === 'closed') {
            closeSidebar();
        } else {
            openSidebar();
        }
    }
    // Mobile: always starts closed (CSS default -translate-x-full)

    // Stagger animation delays for slide-up cards
    document.querySelectorAll('.animate-slide-up').forEach((el, i) => {
        el.style.animationDelay = `${i * 0.07}s`;
    });

    // Keyboard shortcut Ctrl+K → focus search
    document.addEventListener('keydown', e => {
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            document.querySelector('input[type="text"]')?.focus();
        }
        // Esc → close sidebar on mobile
        if (e.key === 'Escape' && isMobile()) closeSidebar();
    });

    // Responsive: if window resizes from mobile to desktop, ensure overlay hidden
    window.addEventListener('resize', () => {
        if (!isMobile()) {
            const overlay = document.getElementById('sidebar-overlay');
            overlay.classList.add('hidden');
            overlay.classList.replace('opacity-100', 'opacity-0');
        }
    });

});
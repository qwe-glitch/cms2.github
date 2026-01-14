// ===================================================================
// Dark Mode Toggle - JavaScript
// Handles switching between light and dark mode with persistence
// ===================================================================

(function () {
    'use strict';

    // DOM Elements
    const darkModeToggle = document.getElementById('darkModeToggle');
    const toggleSwitch = document.getElementById('toggleSwitch');
    const lightIcon = document.getElementById('lightIcon');
    const darkIcon = document.getElementById('darkIcon');
    const body = document.body;

    // Storage key for dark mode preference
    const DARK_MODE_KEY = 'darkModeEnabled';

    // Initialize dark mode based on saved preference
    function initDarkMode() {
        const savedPreference = localStorage.getItem(DARK_MODE_KEY);

        if (savedPreference === 'true') {
            enableDarkMode();
        } else {
            disableDarkMode();
        }
    }

    // Enable dark mode
    function enableDarkMode() {
        body.classList.add('dark-mode');
        toggleSwitch.classList.add('active');
        localStorage.setItem(DARK_MODE_KEY, 'true');
        updateIcons(true);
    }

    // Disable dark mode
    function disableDarkMode() {
        body.classList.remove('dark-mode');
        toggleSwitch.classList.remove('active');
        localStorage.setItem(DARK_MODE_KEY, 'false');
        updateIcons(false);
    }

    // Update icon visibility
    function updateIcons(isDarkMode) {
        if (isDarkMode) {
            lightIcon.style.opacity = '0.4';
            darkIcon.style.opacity = '1';
            darkIcon.style.color = '#c084fc';
        } else {
            lightIcon.style.opacity = '1';
            lightIcon.style.color = '#fbbf24';
            darkIcon.style.opacity = '0.4';
        }
    }

    // Toggle dark mode
    function toggleDarkMode() {
        if (body.classList.contains('dark-mode')) {
            disableDarkMode();
        } else {
            enableDarkMode();
        }
    }

    // Add click event listener
    if (darkModeToggle) {
        darkModeToggle.addEventListener('click', toggleDarkMode);
    }

    // Initialize on page load
    document.addEventListener('DOMContentLoaded', initDarkMode);

    // Also run immediately in case DOM is already loaded
    if (document.readyState === 'complete' || document.readyState === 'interactive') {
        initDarkMode();
    }
})();

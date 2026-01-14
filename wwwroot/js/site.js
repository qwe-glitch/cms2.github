/* ===================================================================
   Complaint Management System - JavaScript Utilities
   Author: Admin
   Description: Reusable JavaScript utilities for enhanced UX
   =================================================================== */

(function () {
    'use strict';

    // ===================================================================
    // Toast Notification System
    // ===================================================================
    const Toast = {
        container: null,

        init() {
            if (!this.container) {
                this.container = document.createElement('div');
                this.container.className = 'toast-container';
                document.body.appendChild(this.container);
            }
        },

        show(message, type = 'info', duration = 4000) {
            this.init();

            const toast = document.createElement('div');
            toast.className = `toast toast-${type} animate-slide-in-right`;

            const icons = {
                success: 'bi-check-circle-fill',
                error: 'bi-x-circle-fill',
                warning: 'bi-exclamation-triangle-fill',
                info: 'bi-info-circle-fill'
            };

            const titles = {
                success: 'Success',
                error: 'Error',
                warning: 'Warning',
                info: 'Information'
            };

            toast.innerHTML = `
                <i class="toast-icon bi ${icons[type]} text-${type === 'error' ? 'danger' : type}"></i>
                <div class="toast-content">
                    <div class="toast-title">${titles[type]}</div>
                    <p class="toast-message">${message}</p>
                </div>
                <button class="toast-close" aria-label="Close">
                    <i class="bi bi-x"></i>
                </button>
            `;

            this.container.appendChild(toast);

            // Close button handler
            const closeBtn = toast.querySelector('.toast-close');
            closeBtn.addEventListener('click', () => this.remove(toast));

            // Auto remove after duration
            if (duration > 0) {
                setTimeout(() => this.remove(toast), duration);
            }

            return toast;
        },

        remove(toast) {
            toast.style.animation = 'slideInRight 0.3s ease-out reverse';
            setTimeout(() => {
                if (toast.parentNode) {
                    toast.parentNode.removeChild(toast);
                }
            }, 300);
        },

        success(message, duration) {
            return this.show(message, 'success', duration);
        },

        error(message, duration) {
            return this.show(message, 'error', duration);
        },

        warning(message, duration) {
            return this.show(message, 'warning', duration);
        },

        info(message, duration) {
            return this.show(message, 'info', duration);
        }
    };

    // Make Toast globally available
    window.Toast = Toast;

    // ===================================================================
    // Confirmation Dialog
    // ===================================================================
    window.confirmDialog = function (message, title = 'Confirm Action', onConfirm, onCancel) {
        const modal = document.createElement('div');
        modal.className = 'modal fade';
        modal.setAttribute('tabindex', '-1');
        modal.innerHTML = `
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content">
                    <div class="modal-header border-0">
                        <h5 class="modal-title fw-bold">${title}</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <p class="mb-0">${message}</p>
                    </div>
                    <div class="modal-footer border-0">
                        <button type="button" class="btn btn-light" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn btn-primary" id="confirmBtn">Confirm</button>
                    </div>
                </div>
            </div>
        `;

        document.body.appendChild(modal);
        const bsModal = new bootstrap.Modal(modal);

        const confirmBtn = modal.querySelector('#confirmBtn');
        confirmBtn.addEventListener('click', () => {
            if (onConfirm) onConfirm();
            bsModal.hide();
        });

        modal.addEventListener('hidden.bs.modal', () => {
            if (onCancel) onCancel();
            modal.remove();
        });

        bsModal.show();
        return bsModal;
    };

    // ===================================================================
    // Form Validation Enhancement
    // ===================================================================
    const FormValidator = {
        init(formSelector) {
            const forms = document.querySelectorAll(formSelector);
            forms.forEach(form => {
                form.addEventListener('submit', (e) => {
                    if (!form.checkValidity()) {
                        e.preventDefault();
                        e.stopPropagation();
                        Toast.error('Please fill in all required fields correctly.');
                    }
                    form.classList.add('was-validated');
                }, false);

                // Real-time validation - only show validation after field has content
                const inputs = form.querySelectorAll('input, textarea, select');
                inputs.forEach(input => {
                    let hasBeenTouched = false;

                    input.addEventListener('blur', () => {
                        hasBeenTouched = true;
                        // Only show validation if field has content or is required
                        if (input.value.trim() !== '' || input.hasAttribute('required')) {
                            if (input.checkValidity() && input.value.trim() !== '') {
                                input.classList.remove('is-invalid');
                                input.classList.add('is-valid');
                            } else if (input.value.trim() !== '') {
                                input.classList.remove('is-valid');
                                input.classList.add('is-invalid');
                            }
                        }
                    });

                    input.addEventListener('input', () => {
                        // Only validate if the field has been touched or has validation classes
                        if (hasBeenTouched || input.classList.contains('is-valid') || input.classList.contains('is-invalid')) {
                            if (input.value.trim() === '') {
                                // Clear validation if field is empty
                                input.classList.remove('is-valid', 'is-invalid');
                            } else if (input.checkValidity()) {
                                input.classList.remove('is-invalid');
                                input.classList.add('is-valid');
                            } else {
                                input.classList.remove('is-valid');
                                input.classList.add('is-invalid');
                            }
                        }
                    });
                });
            });
        }
    };

    window.FormValidator = FormValidator;

    // ===================================================================
    // Loading Overlay
    // ===================================================================
    const LoadingOverlay = {
        overlay: null,

        show(message = 'Loading...') {
            if (!this.overlay) {
                this.overlay = document.createElement('div');
                this.overlay.className = 'loading-overlay';
                this.overlay.innerHTML = `
                    <div class="text-center">
                        <div class="spinner mb-3"></div>
                        <p class="fw-semibold text-gray-700">${message}</p>
                    </div>
                `;
                document.body.appendChild(this.overlay);
            }
            this.overlay.style.display = 'flex';
        },

        hide() {
            if (this.overlay) {
                this.overlay.style.display = 'none';
            }
        },

        remove() {
            if (this.overlay && this.overlay.parentNode) {
                this.overlay.parentNode.removeChild(this.overlay);
                this.overlay = null;
            }
        }
    };

    window.LoadingOverlay = LoadingOverlay;

    // ===================================================================
    // Scroll to Top Button
    // ===================================================================
    function initScrollToTop() {
        const scrollBtn = document.createElement('button');
        scrollBtn.className = 'scroll-to-top';
        scrollBtn.innerHTML = '<i class="bi bi-arrow-up"></i>';
        scrollBtn.setAttribute('aria-label', 'Scroll to top');
        document.body.appendChild(scrollBtn);

        window.addEventListener('scroll', () => {
            if (window.pageYOffset > 300) {
                scrollBtn.classList.add('visible');
            } else {
                scrollBtn.classList.remove('visible');
            }
        });

        scrollBtn.addEventListener('click', () => {
            window.scrollTo({
                top: 0,
                behavior: 'smooth'
            });
        });
    }

    // ===================================================================
    // Smooth Scroll for Anchor Links
    // ===================================================================
    function initSmoothScroll() {
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', function (e) {
                const href = this.getAttribute('href');
                if (href !== '#' && href !== '') {
                    const target = document.querySelector(href);
                    if (target) {
                        e.preventDefault();
                        target.scrollIntoView({
                            behavior: 'smooth',
                            block: 'start'
                        });
                    }
                }
            });
        });
    }

    // ===================================================================
    // Auto-save for Forms
    // ===================================================================
    const AutoSave = {
        init(formSelector, storageKey, interval = 30000) {
            const form = document.querySelector(formSelector);
            if (!form) return;

            // Load saved data
            this.loadSavedData(form, storageKey);

            // Auto-save on input
            let saveTimeout;
            form.addEventListener('input', () => {
                clearTimeout(saveTimeout);
                saveTimeout = setTimeout(() => {
                    this.saveFormData(form, storageKey);
                    Toast.info('Draft saved automatically', 2000);
                }, 2000);
            });

            // Periodic save
            setInterval(() => {
                this.saveFormData(form, storageKey);
            }, interval);

            // Clear on submit
            form.addEventListener('submit', () => {
                this.clearSavedData(storageKey);
            });
        },

        saveFormData(form, storageKey) {
            const formData = new FormData(form);
            const data = {};
            formData.forEach((value, key) => {
                data[key] = value;
            });
            localStorage.setItem(storageKey, JSON.stringify(data));
        },

        loadSavedData(form, storageKey) {
            const savedData = localStorage.getItem(storageKey);
            if (savedData) {
                const data = JSON.parse(savedData);
                Object.keys(data).forEach(key => {
                    const input = form.querySelector(`[name="${key}"]`);
                    if (input) {
                        input.value = data[key];
                    }
                });
                Toast.info('Draft restored', 2000);
            }
        },

        clearSavedData(storageKey) {
            localStorage.removeItem(storageKey);
        }
    };

    window.AutoSave = AutoSave;

    // ===================================================================
    // Utility Functions
    // ===================================================================

    // Debounce function
    window.debounce = function (func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    };

    // Throttle function
    window.throttle = function (func, limit) {
        let inThrottle;
        return function (...args) {
            if (!inThrottle) {
                func.apply(this, args);
                inThrottle = true;
                setTimeout(() => inThrottle = false, limit);
            }
        };
    };

    // Format date
    window.formatDate = function (date, format = 'medium') {
        const options = {
            short: { month: 'short', day: 'numeric' },
            medium: { month: 'short', day: 'numeric', year: 'numeric' },
            long: { month: 'long', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' }
        };
        return new Date(date).toLocaleDateString('en-US', options[format] || options.medium);
    };

    // Copy to clipboard
    window.copyToClipboard = function (text) {
        if (navigator.clipboard) {
            navigator.clipboard.writeText(text).then(() => {
                Toast.success('Copied to clipboard!', 2000);
            }).catch(() => {
                Toast.error('Failed to copy');
            });
        } else {
            // Fallback for older browsers
            const textarea = document.createElement('textarea');
            textarea.value = text;
            textarea.style.position = 'fixed';
            textarea.style.opacity = '0';
            document.body.appendChild(textarea);
            textarea.select();
            try {
                document.execCommand('copy');
                Toast.success('Copied to clipboard!', 2000);
            } catch (err) {
                Toast.error('Failed to copy');
            }
            document.body.removeChild(textarea);
        }
    };

    // Character counter for textareas
    function initCharacterCounters() {
        document.querySelectorAll('textarea[maxlength]').forEach(textarea => {
            const maxLength = textarea.getAttribute('maxlength');
            
            // Skip if a counter already exists (check for sibling or parent containing counter text)
            const parent = textarea.parentNode;
            const existingCounter = parent.querySelector(`[id$="Count"], small:has(span[id$="Count"])`);
            if (existingCounter || parent.innerHTML.includes(`/ ${maxLength}`)) {
                return; // Counter already exists, skip this textarea
            }
            
            const counter = document.createElement('div');
            counter.className = 'text-muted small mt-1 text-end';
            counter.innerHTML = `<span class="current">0</span> / ${maxLength}`;

            textarea.parentNode.insertBefore(counter, textarea.nextSibling);

            const currentSpan = counter.querySelector('.current');

            textarea.addEventListener('input', () => {
                const length = textarea.value.length;
                currentSpan.textContent = length;

                if (length > maxLength * 0.9) {
                    counter.classList.add('text-warning');
                } else {
                    counter.classList.remove('text-warning');
                }
            });

            // Trigger initial count
            textarea.dispatchEvent(new Event('input'));
        });
    }

    // Password visibility toggle
    function initPasswordToggle() {
        document.querySelectorAll('input[type="password"]').forEach(input => {
            const wrapper = input.parentElement;
            if (wrapper.classList.contains('input-group')) {
                const toggleBtn = document.createElement('button');
                toggleBtn.className = 'btn btn-outline-secondary';
                toggleBtn.type = 'button';
                toggleBtn.innerHTML = '<i class="bi bi-eye"></i>';

                toggleBtn.addEventListener('click', () => {
                    if (input.type === 'password') {
                        input.type = 'text';
                        toggleBtn.innerHTML = '<i class="bi bi-eye-slash"></i>';
                    } else {
                        input.type = 'password';
                        toggleBtn.innerHTML = '<i class="bi bi-eye"></i>';
                    }
                });

                wrapper.appendChild(toggleBtn);
            }
        });
    }

    // Active navigation highlighting
    function highlightActiveNav() {
        const currentPath = window.location.pathname;

        // First, try to find an exact match
        let exactMatchFound = false;
        document.querySelectorAll('.navbar .nav-link, .dropdown-item').forEach(link => {
            const href = link.getAttribute('href');
            if (href && href === currentPath) {
                link.classList.add('active');
                exactMatchFound = true;
                // If it's a dropdown item, also mark the parent dropdown as active
                const dropdown = link.closest('.dropdown');
                if (dropdown) {
                    dropdown.querySelector('.dropdown-toggle').classList.add('active');
                }
            }
        });

        // If no exact match, fall back to prefix matching but only for deeper paths
        // Skip for main routes like /Complaint when we're on /Complaint/MyWork
        if (!exactMatchFound) {
            document.querySelectorAll('.navbar .nav-link, .dropdown-item').forEach(link => {
                const href = link.getAttribute('href');
                // Only use prefix matching if the href has more segments than just the controller
                // e.g., /Complaint/Details would match /Complaint/Details/5
                // But /Complaint would NOT match /Complaint/MyWork
                if (href && href !== '/' && href.split('/').filter(x => x).length >= 2 && currentPath.startsWith(href)) {
                    link.classList.add('active');
                    const dropdown = link.closest('.dropdown');
                    if (dropdown) {
                        dropdown.querySelector('.dropdown-toggle').classList.add('active');
                    }
                }
            });
        }

    }



    // Animate elements on scroll
    function initScrollAnimations() {
        const observerOptions = {
            threshold: 0.1,
            rootMargin: '0px 0px -50px 0px'
        };

        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('animate-fade-in-up');
                    observer.unobserve(entry.target);
                }
            });
        }, observerOptions);

        document.querySelectorAll('.card, .stat-card, .complaint-card').forEach(el => {
            observer.observe(el);
        });
    }

    // Initialize tooltips
    function initTooltips() {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }

    // Initialize popovers
    function initPopovers() {
        const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
        popoverTriggerList.map(function (popoverTriggerEl) {
            return new bootstrap.Popover(popoverTriggerEl);
        });
    }

    // Add loading state to buttons on form submit
    function initButtonLoadingStates() {
        document.querySelectorAll('form').forEach(form => {
            form.addEventListener('submit', function (e) {
                const submitBtn = form.querySelector('button[type="submit"]');
                if (submitBtn && form.checkValidity()) {
                    submitBtn.disabled = true;
                    const originalText = submitBtn.innerHTML;
                    submitBtn.innerHTML = '<span class="spinner spinner-sm me-2"></span> Processing...';

                    // Re-enable after 10 seconds as fallback
                    setTimeout(() => {
                        submitBtn.disabled = false;
                        submitBtn.innerHTML = originalText;
                    }, 10000);
                }
            });
        });
    }

    // Table search functionality
    window.initTableSearch = function (searchInputId, tableId) {
        const searchInput = document.getElementById(searchInputId);
        const table = document.getElementById(tableId);

        if (!searchInput || !table) return;

        searchInput.addEventListener('input', debounce(function () {
            const searchTerm = this.value.toLowerCase();
            const rows = table.querySelectorAll('tbody tr');

            let visibleCount = 0;
            rows.forEach(row => {
                const text = row.textContent.toLowerCase();
                if (text.includes(searchTerm)) {
                    row.style.display = '';
                    visibleCount++;
                } else {
                    row.style.display = 'none';
                }
            });

            // Show no results message
            let noResultsRow = table.querySelector('.no-results');
            if (visibleCount === 0) {
                if (!noResultsRow) {
                    noResultsRow = document.createElement('tr');
                    noResultsRow.className = 'no-results';
                    noResultsRow.innerHTML = `<td colspan="100" class="text-center text-muted py-4">No results found</td>`;
                    table.querySelector('tbody').appendChild(noResultsRow);
                }
            } else if (noResultsRow) {
                noResultsRow.remove();
            }
        }, 300));
    };

    // ===================================================================
    // DOM Ready Initialization
    // ===================================================================
    document.addEventListener('DOMContentLoaded', function () {
        // Initialize all features
        initScrollToTop();
        initSmoothScroll();
        initCharacterCounters();
        initPasswordToggle();
        highlightActiveNav();
        initScrollAnimations();
        initTooltips();
        initPopovers();
        initButtonLoadingStates();

        // Initialize form validation for all forms with class 'needs-validation'
        FormValidator.init('.needs-validation');

        // Log initialization
        console.log('%c✓ CMS JavaScript Utilities Loaded', 'color: #10b981; font-weight: bold; font-size: 14px;');
        console.log('%cToast, LoadingOverlay, FormValidator, AutoSave, and utility functions are available globally.', 'color: #64748b; font-size: 12px;');
    });

})();

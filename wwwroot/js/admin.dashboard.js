// Admin Dashboard JavaScript
// Note: Counter animation is called from the inline script in Dashboard.cshtml
// because it needs access to Razor Model values

document.addEventListener('DOMContentLoaded', function () {
    // Animate counters function
    window.animateCounter = function (element, target) {
        let current = 0;
        const increment = target / 50;
        const timer = setInterval(() => {
            current += increment;
            if (current >= target) {
                element.textContent = target;
                clearInterval(timer);
            } else {
                element.textContent = Math.floor(current);
            }
        }, 20);
    };
});

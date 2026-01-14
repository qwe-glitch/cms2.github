document.addEventListener('DOMContentLoaded', function () {
    console.log("Intro.js (standalone mode) starting...");

    // 1. Get DOM elements
    const sliderWrapper = document.querySelector('.intro-slider-wrapper');
    const allSlides = document.querySelectorAll('.intro-content');
    const btn1 = document.getElementById('nextSlideButton1');
    const btn2 = document.getElementById('nextSlideButton2');
    const btnReset = document.getElementById('resetSlideButton');

    // 2. Initialize and display first page
    setTimeout(() => {
        if (allSlides[0]) allSlides[0].classList.add('is-loaded');
    }, 100);

    // ==========================================
    // Pure slide transition logic (no model interference)
    // ==========================================

    // Button 1: Go to page 2
    if (btn1) {
        btn1.addEventListener('click', () => {
            // Hide current page
            allSlides[0].classList.remove('is-loaded');
            // Move slider
            sliderWrapper.classList.add('show-slide-2');
            // Show next page (delay a bit for slider to reach position)
            setTimeout(() => allSlides[1].classList.add('is-loaded'), 800);
        });
    }

    // Button 2: Go to page 3
    if (btn2) {
        btn2.addEventListener('click', () => {
            allSlides[1].classList.remove('is-loaded');
            sliderWrapper.classList.remove('show-slide-2');
            sliderWrapper.classList.add('show-slide-3');
            setTimeout(() => allSlides[2].classList.add('is-loaded'), 800);
        });
    }

    // Button 3: Reset to page 1
    if (btnReset) {
        btnReset.addEventListener('click', () => {
            allSlides[2].classList.remove('is-loaded');
            // Remove all movement classes, CSS will automatically snap it back to top
            sliderWrapper.classList.remove('show-slide-2', 'show-slide-3');
            setTimeout(() => allSlides[0].classList.add('is-loaded'), 800);
        });
    }
});
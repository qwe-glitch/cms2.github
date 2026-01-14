document.addEventListener('DOMContentLoaded', function () {
    console.log("Announcement JS loaded.");

    // =========================================
    // A. Filter Buttons - Optimized
    // =========================================
    const filterBtns = document.querySelectorAll('.filter-btn');
    const cards = document.querySelectorAll('.bulletin-card');

    // A. Filter Functionality
    if (filterBtns.length > 0 && cards.length > 0) {
        filterBtns.forEach(btn => {
            btn.addEventListener('click', () => {
                // Toggle button styles
                filterBtns.forEach(b => {
                    b.classList.remove('btn-info', 'text-white');
                    b.classList.add('btn-outline-light', 'text-white-50');
                });
                btn.classList.remove('btn-outline-light', 'text-white-50');
                btn.classList.add('btn-info', 'text-white');

                // Key fix: Get filter value and convert to lowercase
                const filterValue = btn.getAttribute('data-filter').trim().toLowerCase();

                cards.forEach(card => {
                    // Key fix: Get value from data-display-text attribute (already set to lowercase in HTML)
                    const categoryText = card.getAttribute('data-display-text') || '';

                    // Comparison logic
                    if (filterValue === 'all' || categoryText === filterValue) {
                        card.style.display = 'flex';
                        card.style.opacity = '0';
                        setTimeout(() => card.style.opacity = '1', 50);
                    } else {
                        card.style.display = 'none';
                    }
                });
            });
        });
    }


    // =========================================
    // B. Admin Edit Modal - Populate Modal Data
    // =========================================
    var editModal = document.getElementById('editSystemModal');
    if (editModal) {
        editModal.addEventListener('show.bs.modal', function (event) {
            var button = event.relatedTarget;
            var title = button.getAttribute('data-title');
            var content = button.getAttribute('data-content');

            document.getElementById('modalTitleInput').value = title;
            document.getElementById('modalContentInput').value = content;
        });
    }

    // =========================================
    // C. Construction Ticker Animation
    // Fix: Listen for animation end, then trigger slide out
    // =========================================
    var ticker = document.getElementById('maintenanceTicker');
    var scrollingText = document.querySelector('#maintenanceTicker .scrolling-text'); // Get scrolling text element

    if (ticker && scrollingText) {
        // 1. Slide in after 1 second delay
        setTimeout(function () {
            ticker.classList.add('show');
        }, 1000);

        scrollingText.addEventListener('animationend', function () {
            console.log("Marquee animation finished. Starting slide out to the RIGHT.");

            // After marquee animation ends, delay 0.5s, then slide the entire bar to the right
            setTimeout(function () {
                // Key fix: Slide the entire bar out to the right side of the screen

                // 1. Use the original right property transition (already defined as 1s in CSS)
                // We just need to remove the .show class to return to CSS default right: -100% state
                ticker.classList.remove('show');

                // 2. Remove old inline styles that may cause conflicts
                ticker.style.transition = ''; // Clear inline transition
                ticker.style.right = ''; // Clear inline right 
                ticker.style.left = ''; // Clear inline left

                // 3. Remove from DOM after fully sliding out
                // Slide out animation time is set to 1s in CSS, so we wait 1 second
                setTimeout(function () {
                    ticker.remove();
                    console.log("Ticker bar removed from DOM.");
                }, 1000); // 1 second is the slide out animation time

            }, 500); // Start sliding out 0.5s after animation ends
        }, { once: true });
    }

});

// =========================================
// D. 3D Data Sphere (Core Math Logic)
// =========================================
// ...
document.addEventListener('DOMContentLoaded', function () {
    const container = document.getElementById('sphereContainer');
    if (!container) return;

    const items = container.querySelectorAll('.sphere-item');

    // Adjust radius here!
    // Previously 220, now set to around 140 (adjust based on model size)
    // If too crowded, use 160; if not close enough, use 120
    const radius = 140;


    // Rotation speed control
    let rotation = { x: 0, y: 0 };
    let targetRotation = { x: 0.001, y: 0.001 }; // Initial slow auto-rotation
    let isHovering = false;

    // 1. Initialize positions (Fibonacci sphere distribution)
    // This algorithm distributes points most evenly on the sphere surface
    const count = items.length;
    const phi = Math.PI * (3 - Math.sqrt(5)); // Golden angle

    items.forEach((item, i) => {
        const y = 1 - (i / (count - 1)) * 2; // y goes from 1 to -1
        const radiusAtY = Math.sqrt(1 - y * y); // Radius at current height
        const theta = phi * i; // Golden angle spiral

        const x = Math.cos(theta) * radiusAtY;
        const z = Math.sin(theta) * radiusAtY;

        // Store initial 3D coordinates
        item.dataset.x = x * radius;
        item.dataset.y = y * radius;
        item.dataset.z = z * radius;

        item.style.visibility = 'visible'; // Show after calculation complete
    });

    // 2. Animation loop
    function animate() {
        // Easing effect: current speed gradually approaches target speed
        rotation.x += (targetRotation.x - rotation.x) * 0.05;
        rotation.y += (targetRotation.y - rotation.y) * 0.05;

        // Base auto-rotation (if no mouse interference)
        if (!isHovering) {
            rotation.y += 0.002;
        }

        // Rotation matrix calculation
        const cosX = Math.cos(rotation.x);
        const sinX = Math.sin(rotation.x);
        const cosY = Math.cos(rotation.y);
        const sinY = Math.sin(rotation.y);

        items.forEach(item => {
            let x = parseFloat(item.dataset.x);
            let y = parseFloat(item.dataset.y);
            let z = parseFloat(item.dataset.z);

            // Rotate around Y axis
            let rx1 = x * cosY - z * sinY;
            let rz1 = z * cosY + x * sinY;

            // Rotate around X axis
            let ry2 = y * cosX - rz1 * sinX;
            let rz2 = rz1 * cosX + y * sinX;

            // Update coordinates
            item.dataset.x = rx1;
            item.dataset.y = ry2;
            item.dataset.z = rz2;

            // 3D to 2D projection (Perspective Projection)
            // 1000 is the perspective depth, larger z means farther
            const scale = 1000 / (1000 - rz2);
            const alpha = (rz2 + radius) / (2 * radius); // Opacity based on depth

            // Apply styles
            item.style.transform = `translate3d(${rx1}px, ${ry2}px, 0) scale(${scale})`;
            item.style.opacity = Math.max(0.2, alpha); // Farther items fade out
            item.style.zIndex = Math.floor(alpha * 100); // Farther items at lower z-index

            // Important: Keep text from flipping (Billboarding)
            // Although it's 3D rotation, we want text to always face the screen.
            // No need for reverse rotation since translate3d only moves position, not rotates the element.
        });

        requestAnimationFrame(animate);
    }

    animate();

    // 3. Mouse interaction (mouse movement controls rotation direction)
    const wrapper = document.querySelector('.sphere-wrapper');

    wrapper.addEventListener('mousemove', (e) => {
        isHovering = true;
        const rect = wrapper.getBoundingClientRect();
        // Calculate mouse offset relative to container center (-1 to 1)
        const mouseX = (e.clientX - rect.left - rect.width / 2) / (rect.width / 2);
        const mouseY = (e.clientY - rect.top - rect.height / 2) / (rect.height / 2);

        // Update target rotation speed
        targetRotation.y = mouseX * 0.05; // Mouse left/right controls Y axis rotation
        targetRotation.x = -mouseY * 0.05; // Mouse up/down controls X axis rotation
    });

    wrapper.addEventListener('mouseleave', () => {
        isHovering = false;
        targetRotation = { x: 0.001, y: 0.002 }; // Restore default slow auto-rotation
    });
});
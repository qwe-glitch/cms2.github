// Complaint Index JavaScript

document.addEventListener('DOMContentLoaded', function() {
    const searchInput = document.getElementById('searchInput');
    const statusFilter = document.getElementById('statusFilter');
    const priorityFilter = document.getElementById('priorityFilter');
    const gridViewBtn = document.getElementById('gridView');
    const listViewBtn = document.getElementById('listView');
    const complaintsContainer = document.getElementById('complaintsContainer');
    const items = document.querySelectorAll('.complaint-item');
    const noResults = document.getElementById('noResults');

    // Filter function
    function filterComplaints() {
        const searchTerm = searchInput?.value.toLowerCase() || '';
        const statusValue = statusFilter?.value.toLowerCase() || '';
        const priorityValue = priorityFilter?.value.toLowerCase() || '';
        
        let visibleCount = 0;

        items.forEach(item => {
            const title = item.dataset.title;
            const status = item.dataset.status;
            const priority = item.dataset.priority;
            const category = item.dataset.category;

            const matchesSearch = !searchTerm || title.includes(searchTerm) || category.includes(searchTerm);
            const matchesStatus = !statusValue || status === statusValue;
            const matchesPriority = !priorityValue || priority === priorityValue;

            if (matchesSearch && matchesStatus && matchesPriority) {
                item.classList.remove('d-none');
                visibleCount++;
            } else {
                item.classList.add('d-none');
            }
        });

        // Show/hide no results message
        if (noResults) {
            if (visibleCount === 0 && items.length > 0) {
                noResults.classList.remove('d-none');
                complaintsContainer?.classList.add('d-none');
            } else {
                noResults.classList.add('d-none');
                complaintsContainer?.classList.remove('d-none');
            }
        }
    }

    // View toggle
    function toggleView(isGrid) {
        const items = complaintsContainer?.querySelectorAll('.complaint-item');
        
        if (isGrid) {
            // Switch to grid view
            items?.forEach(item => {
                item.classList.remove('col-12');
                item.classList.add('col-md-6', 'col-lg-4');
            });
            gridViewBtn?.classList.add('active');
            listViewBtn?.classList.remove('active');
        } else {
            // Switch to list view
            items?.forEach(item => {
                item.classList.remove('col-md-6', 'col-lg-4');
                item.classList.add('col-12');
            });
            gridViewBtn?.classList.remove('active');
            listViewBtn?.classList.add('active');
        }
    }

    // Event listeners
    searchInput?.addEventListener('input', debounce(filterComplaints, 300));
    statusFilter?.addEventListener('change', filterComplaints);
    priorityFilter?.addEventListener('change', filterComplaints);
    gridViewBtn?.addEventListener('click', () => toggleView(true));
    listViewBtn?.addEventListener('click', () => toggleView(false));

    // Global reset function
    window.resetFilters = function() {
        if (searchInput) searchInput.value = '';
        if (statusFilter) statusFilter.value = '';
        if (priorityFilter) priorityFilter.value = '';
        filterComplaints();
    };
});

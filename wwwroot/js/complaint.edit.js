// Complaint Edit JavaScript (based on Create, but without auto-save)

document.addEventListener('DOMContentLoaded', function () {
    const titleInput = document.querySelector('input[name="Title"]');
    const descInput = document.querySelector('textarea[name="Description"]');
    const titleCount = document.getElementById('titleCount');
    const descCount = document.getElementById('descCount');
    const fileInput = document.getElementById('fileInput');
    const filePreview = document.getElementById('filePreview');
    const btnLocate = document.getElementById('btnLocate');
    const locationInput = document.getElementById('locationInput');
    const latInput = document.getElementById('latitude');
    const longInput = document.getElementById('longitude');
    const locationSuggestions = document.getElementById('locationSuggestions');

    // Knowledge Base elements
    const kbSuggestions = document.getElementById('knowledgeBaseSuggestions');
    const suggestionsList = document.getElementById('suggestionsList');
    const dismissBtn = document.getElementById('dismissSuggestions');
    const categorySelect = document.querySelector('select[name="CategoryId"]');
    let kbSearchTimeout;
    let dismissed = false;

    // Character counters
    if (titleInput && titleCount) {
        titleInput.addEventListener('input', () => {
            titleCount.textContent = titleInput.value.length;

            // Knowledge Base Search - debounced
            if (!dismissed && titleInput.value.length >= 3) {
                clearTimeout(kbSearchTimeout);
                kbSearchTimeout = setTimeout(() => searchKnowledgeBase(titleInput.value), 500);
            } else if (titleInput.value.length < 3) {
                hideKnowledgeBaseSuggestions();
            }
        });
    }

    // Dismiss knowledge base suggestions
    if (dismissBtn) {
        dismissBtn.addEventListener('click', () => {
            dismissed = true;
            hideKnowledgeBaseSuggestions();
        });
    }

    // Knowledge Base Search Function
    async function searchKnowledgeBase(query) {
        if (!kbSuggestions || !suggestionsList) return;

        const categoryId = categorySelect?.value || '';
        const url = `/Complaint/SearchKnowledgeBase?query=${encodeURIComponent(query)}${categoryId ? `&categoryId=${categoryId}` : ''}`;

        try {
            const response = await fetch(url);
            const data = await response.json();

            if (data.suggestions && data.suggestions.length > 0) {
                displayKnowledgeBaseSuggestions(data.suggestions);
            } else {
                hideKnowledgeBaseSuggestions();
            }
        } catch (error) {
            console.error('Knowledge base search error:', error);
            hideKnowledgeBaseSuggestions();
        }
    }

    function displayKnowledgeBaseSuggestions(suggestions) {
        if (!suggestionsList || !kbSuggestions) return;

        suggestionsList.innerHTML = suggestions.map(s => `
            <div class="kb-suggestion-card" onclick="window.open('/Complaint/Details/${s.complaintId}', '_blank')">
                <div class="d-flex align-items-start">
                    <div class="kb-match-badge me-2">
                        <i class="bi bi-check-circle-fill text-success"></i>
                    </div>
                    <div class="flex-grow-1">
                        <div class="fw-semibold text-dark mb-1">${escapeHtml(s.title)}</div>
                        <div class="small text-muted mb-1">${escapeHtml(s.description)}</div>
                        ${s.resolutionSummary ? `<div class="small text-success"><i class="bi bi-check me-1"></i><strong>Resolution:</strong> ${escapeHtml(s.resolutionSummary)}</div>` : ''}
                        <div class="d-flex align-items-center gap-2 mt-2">
                            <span class="badge bg-secondary">${escapeHtml(s.categoryName)}</span>
                            <span class="small text-muted"><i class="bi bi-calendar me-1"></i>${s.resolvedAt}</span>
                        </div>
                    </div>
                    <div class="kb-view-link">
                        <i class="bi bi-box-arrow-up-right text-primary"></i>
                    </div>
                </div>
            </div>
        `).join('');

        kbSuggestions.style.display = 'block';
    }

    function hideKnowledgeBaseSuggestions() {
        if (kbSuggestions) {
            kbSuggestions.style.display = 'none';
        }
    }

    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    if (descInput && descCount) {
        descInput.addEventListener('input', () => {
            descCount.textContent = descInput.value.length;
        });
    }

    // Geolocation
    if (btnLocate) {
        btnLocate.addEventListener('click', function () {
            if (navigator.geolocation) {
                btnLocate.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>';
                navigator.geolocation.getCurrentPosition(function (position) {
                    const lat = position.coords.latitude;
                    const lon = position.coords.longitude;

                    // Reverse geocoding using Nominatim
                    fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lon}`)
                        .then(response => response.json())
                        .then(data => {
                            if (data.display_name) {
                                locationInput.value = data.display_name;
                                if (latInput) latInput.value = lat;
                                if (longInput) longInput.value = lon;
                            }
                            btnLocate.innerHTML = '<i class="bi bi-crosshair"></i>';
                        })
                        .catch(err => {
                            console.error(err);
                            btnLocate.innerHTML = '<i class="bi bi-crosshair"></i>';
                        });
                }, function (error) {
                    alert('Error getting location: ' + error.message);
                    btnLocate.innerHTML = '<i class="bi bi-crosshair"></i>';
                });
            } else {
                alert('Geolocation is not supported by this browser.');
            }
        });
    }

    // Address Autocomplete (Simple debounce)
    let timeoutId;
    if (locationInput) {
        locationInput.addEventListener('input', function () {
            clearTimeout(timeoutId);
            const query = this.value;
            if (query.length < 3) {
                locationSuggestions.style.display = 'none';
                return;
            }

            timeoutId = setTimeout(() => {
                fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}`)
                    .then(response => response.json())
                    .then(data => {
                        locationSuggestions.innerHTML = '';
                        if (data.length > 0) {
                            data.slice(0, 5).forEach(item => {
                                const a = document.createElement('a');
                                a.href = '#';
                                a.className = 'list-group-item list-group-item-action';
                                a.textContent = item.display_name;
                                a.addEventListener('click', function (e) {
                                    e.preventDefault();
                                    locationInput.value = item.display_name;
                                    if (latInput) latInput.value = item.lat;
                                    if (longInput) longInput.value = item.lon;
                                    locationSuggestions.style.display = 'none';
                                });
                                locationSuggestions.appendChild(a);
                            });
                            locationSuggestions.style.display = 'block';
                        } else {
                            locationSuggestions.style.display = 'none';
                        }
                    });
            }, 500);
        });

        // Hide suggestions when clicking outside
        document.addEventListener('click', function (e) {
            if (e.target !== locationInput && e.target !== locationSuggestions) {
                locationSuggestions.style.display = 'none';
            }
        });
    }

    // File preview & EXIF Extraction
    if (fileInput && filePreview) {
        fileInput.addEventListener('change', function (e) {
            filePreview.innerHTML = '';
            const files = e.target.files;

            if (files.length > 0) {
                const previewContainer = document.createElement('div');
                previewContainer.className = 'row g-2';

                Array.from(files).forEach((file, index) => {
                    const col = document.createElement('div');
                    col.className = 'col-6 col-md-4';

                    const card = document.createElement('div');
                    card.className = 'card border shadow-sm';
                    card.innerHTML = `
                        <div class="card-body p-2 text-center">
                            <i class="bi bi-file-earmark text-primary" style="font-size: 2rem;"></i>
                            <p class="small mb-0 mt-1 text-truncate">${file.name}</p>
                            <small class="text-muted">${(file.size / 1024).toFixed(1)} KB</small>
                        </div>
                    `;

                    col.appendChild(card);
                    previewContainer.appendChild(col);

                    // Try to extract EXIF if it's an image and location isn't set
                    if (file.type.startsWith('image/') && !locationInput.value) {
                        EXIF.getData(file, function () {
                            const lat = EXIF.getTag(this, "GPSLatitude");
                            const lon = EXIF.getTag(this, "GPSLongitude");

                            if (lat && lon) {
                                // Convert DMS to DD
                                const latRef = EXIF.getTag(this, "GPSLatitudeRef") || "N";
                                const lonRef = EXIF.getTag(this, "GPSLongitudeRef") || "E";

                                const latDD = (lat[0] + lat[1] / 60 + lat[2] / 3600) * (latRef === "N" ? 1 : -1);
                                const lonDD = (lon[0] + lon[1] / 60 + lon[2] / 3600) * (lonRef === "E" ? 1 : -1);

                                // Reverse geocode
                                fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${latDD}&lon=${lonDD}`)
                                    .then(response => response.json())
                                    .then(data => {
                                        if (data.display_name) {
                                            locationInput.value = data.display_name;
                                            if (latInput) latInput.value = latDD;
                                            if (longInput) longInput.value = lonDD;
                                        }
                                    });
                            }
                        });
                    }
                });

                filePreview.appendChild(previewContainer);
            }
        });
    }

    // ===================================================================
    // Content Validation (Spam, Inappropriate Content, Keyboard Patterns)
    // ===================================================================

    // Sensitive/inappropriate words list (can be expanded as needed)
    const sensitiveWords = [
        'sex', 'fuck', 'shit', 'damn', 'ass', 'bitch', 'bastard', 'crap',
        'dick', 'cock', 'pussy', 'porn', 'xxx', 'nude', 'naked',
        'kill', 'murder', 'rape', 'terrorist', 'bomb', 'drug',
        'idiot', 'stupid', 'dumb', 'retard', 'moron'
    ];

    // Common keyboard patterns (gibberish typed by running fingers across keyboard)
    const keyboardPatterns = [
        'qwert', 'werty', 'ertyu', 'rtyui', 'tyuio', 'yuiop',
        'asdfg', 'sdfgh', 'dfghj', 'fghjk', 'ghjkl',
        'zxcvb', 'xcvbn', 'cvbnm',
        'qazws', 'wsxed', 'edcrf', 'rfvtg', 'tgbyh', 'yhnuj', 'ujmik',
        '12345', '23456', '34567', '45678', '56789', '67890',
        'abcde', 'bcdef', 'cdefg', 'defgh', 'efghi',
        'aaaaa', 'bbbbb', 'ccccc', 'ddddd', 'eeeee'
    ];

    /**
     * Check if text contains sensitive/inappropriate words
     * Returns the found word or null if none found
     */
    function containsSensitiveWord(text) {
        if (!text) return null;
        const lowerText = text.toLowerCase().replace(/[^a-z]/g, ' ');

        for (const word of sensitiveWords) {
            // Check for whole word or as part of text
            const regex = new RegExp(`\\b${word}\\b|${word}`, 'i');
            if (regex.test(lowerText)) {
                return word;
            }
        }
        return null;
    }

    /**
     * Check if text contains keyboard pattern gibberish
     * Returns true if text appears to be keyboard mashing
     */
    function hasKeyboardPattern(text) {
        if (!text || text.length < 5) return false;

        const cleanText = text.toLowerCase().replace(/\s+/g, '');
        if (cleanText.length < 5) return false;

        // Check for keyboard patterns
        let patternCount = 0;
        for (const pattern of keyboardPatterns) {
            if (cleanText.includes(pattern)) {
                patternCount++;
            }
        }

        // If multiple keyboard patterns found, it's likely gibberish
        if (patternCount >= 2) return true;

        // Check if text is mostly keyboard pattern
        for (const pattern of keyboardPatterns) {
            if (cleanText.includes(pattern) && cleanText.length < 20) {
                // Short text with keyboard pattern is likely gibberish
                return true;
            }
        }

        return false;
    }

    /**
     * Check if text contains excessive repeated characters (spam/gibberish detection)
     * Returns true if the text appears to be spam (e.g., "aaaaaaa", "abababab")
     */
    function hasExcessiveRepeatedChars(text) {
        if (!text || text.length < 5) return false;

        // Remove spaces for analysis
        const cleanText = text.replace(/\s+/g, '').toLowerCase();
        if (cleanText.length < 5) return false;

        // Check for single character repeated excessively (e.g., "aaaaaaa")
        // If more than 70% of the text is the same character, it's likely spam
        const charCounts = {};
        for (const char of cleanText) {
            charCounts[char] = (charCounts[char] || 0) + 1;
        }

        const maxCount = Math.max(...Object.values(charCounts));
        const repetitionRatio = maxCount / cleanText.length;

        if (repetitionRatio > 0.7 && cleanText.length >= 10) {
            return true;
        }

        // Check for consecutive repeated characters (e.g., "aaaaaa...")
        // 5 or more consecutive same characters is considered spam
        const consecutivePattern = /(.)\1{4,}/;
        if (consecutivePattern.test(cleanText)) {
            return true;
        }

        // Check for repeated short patterns (e.g., "ababab", "abcabc")
        for (let patternLen = 1; patternLen <= 3; patternLen++) {
            if (cleanText.length >= patternLen * 4) {
                const pattern = cleanText.substring(0, patternLen);
                const repeated = pattern.repeat(Math.floor(cleanText.length / patternLen));
                // If 80% or more matches a repeated pattern
                let matchCount = 0;
                for (let i = 0; i < Math.min(repeated.length, cleanText.length); i++) {
                    if (repeated[i] === cleanText[i]) matchCount++;
                }
                if (matchCount / cleanText.length > 0.8 && cleanText.length >= 10) {
                    return true;
                }
            }
        }

        return false;
    }

    /**
     * Validate field for content issues (spam, sensitive words, keyboard patterns)
     */
    function validateContent(input, fieldName) {
        const value = input.value.trim();
        let errorMessage = null;

        // Check for sensitive words
        const sensitiveWord = containsSensitiveWord(value);
        if (sensitiveWord) {
            errorMessage = `Please enter appropriate content. Inappropriate language is not allowed.`;
        }
        // Check for keyboard pattern gibberish
        else if (hasKeyboardPattern(value)) {
            errorMessage = `Please enter valid ${fieldName}. Random keyboard patterns are not allowed.`;
        }
        // Check for repeated characters
        else if (hasExcessiveRepeatedChars(value)) {
            errorMessage = `Please enter a valid ${fieldName}. Repeated characters or patterns are not allowed.`;
        }

        if (errorMessage) {
            input.classList.add('is-invalid');
            input.classList.remove('is-valid');

            // Add or update error message
            let errorSpan = input.parentElement.querySelector('.content-validation-error');
            if (!errorSpan) {
                errorSpan = document.createElement('span');
                errorSpan.className = 'text-danger small content-validation-error d-block';
                const validationSpan = input.parentElement.querySelector('.text-danger.small');
                if (validationSpan) {
                    validationSpan.parentElement.insertBefore(errorSpan, validationSpan.nextSibling);
                } else {
                    input.parentElement.appendChild(errorSpan);
                }
            }
            errorSpan.textContent = errorMessage;
            return false;
        } else {
            // Clear the error if exists
            const errorSpan = input.parentElement.querySelector('.content-validation-error');
            if (errorSpan) {
                errorSpan.remove();
            }
            if (value.length > 0) {
                input.classList.remove('is-invalid');
            }
            return true;
        }
    }

    // Add real-time validation for title
    if (titleInput) {
        titleInput.addEventListener('blur', () => validateContent(titleInput, 'title'));
        titleInput.addEventListener('input', () => {
            // Only validate on input if already marked invalid
            if (titleInput.classList.contains('is-invalid')) {
                validateContent(titleInput, 'title');
            }
        });
    }

    // Add real-time validation for description
    if (descInput) {
        descInput.addEventListener('blur', () => validateContent(descInput, 'description'));
        descInput.addEventListener('input', () => {
            // Only validate on input if already marked invalid
            if (descInput.classList.contains('is-invalid')) {
                validateContent(descInput, 'description');
            }
        });
    }

    // Form submission validation
    const form = document.getElementById('complaintEditForm');
    if (form) {
        form.addEventListener('submit', function (e) {
            let isValid = true;

            // Validate content in title
            if (titleInput && !validateContent(titleInput, 'title')) {
                isValid = false;
            }

            // Validate content in description
            if (descInput && !validateContent(descInput, 'description')) {
                isValid = false;
            }

            if (!isValid) {
                e.preventDefault();
                e.stopPropagation();
                if (typeof Toast !== 'undefined') {
                    Toast.error('Please provide valid content. Inappropriate language, keyboard patterns, or repeated characters are not allowed.');
                }
                return false;
            }

            if (form.checkValidity()) {
                // Clear auto-save
                if (typeof AutoSave !== 'undefined') {
                    AutoSave.clearSavedData('complaintEditDraft');
                }
            }
        });
    }

    // Auto-save (using the global function from site.js)
    if (typeof AutoSave !== 'undefined') {
        AutoSave.init('#complaintEditForm', 'complaintEditDraft', 60000); // Save every minute
    }

    // ===================================================================
    // Existing Attachments Deletion
    // ===================================================================
    const deleteButtons = document.querySelectorAll('.btn-delete-attachment');
    const deleteListContainer = document.getElementById('deleteAttachmentsList');

    deleteButtons.forEach(btn => {
        btn.addEventListener('click', function () {
            const attachmentId = this.getAttribute('data-id');
            const fileName = this.getAttribute('data-filename') || 'this attachment';
            const item = document.querySelector(`.attachment-item[data-id="${attachmentId}"]`);

            if (typeof confirmDialog === 'function') {
                confirmDialog(`Are you sure you want to remove <strong>${fileName}</strong>?`, 'Confirm Removal', () => {
                    // Add to hidden inputs
                    const input = document.createElement('input');
                    input.type = 'hidden';
                    input.name = 'AttachmentsToDelete';
                    input.value = attachmentId;
                    deleteListContainer.appendChild(input);

                    // Hide from UI
                    if (item) {
                        item.style.transition = 'all 0.3s ease';
                        item.style.opacity = '0';
                        item.style.transform = 'scale(0.8)';
                        setTimeout(() => {
                            item.remove();
                            if (document.querySelectorAll('.attachment-item').length === 0) {
                                const container = document.getElementById('existingAttachmentsContainer');
                                if (container) container.innerHTML = '<div class="col-12"><p class="text-muted italic">All previous attachments marked for removal.</p></div>';
                            }
                        }, 300);
                    }
                });
            } else {
                // Fallback to native confirm if utility is missing
                if (confirm('Are you sure you want to remove this attachment?')) {
                    const input = document.createElement('input');
                    input.type = 'hidden';
                    input.name = 'AttachmentsToDelete';
                    input.value = attachmentId;
                    deleteListContainer.appendChild(input);
                    if (item) item.remove();
                }
            }
        });
    });
});


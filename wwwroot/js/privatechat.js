// Private Chat JavaScript with SignalR
document.addEventListener('DOMContentLoaded', function () {
    // Check if SignalR is loaded
    if (typeof signalR === 'undefined') {
        console.error("SignalR library is not loaded.");
        const messagesDiv = document.getElementById('chatMessages');
        if (messagesDiv) {
            messagesDiv.innerHTML = '<div class="text-center text-danger p-4">Error: Chat library could not be loaded. Please refresh the page.</div>';
        }
        return;
    }

    const currentUserIdInput = document.getElementById('currentUserId');
    const currentUserTypeInput = document.getElementById('currentUserType');
    const currentUserNameInput = document.getElementById('currentUserName');

    if (!currentUserIdInput || !currentUserTypeInput) {
        console.error("User context inputs missing.");
        return;
    }

    const currentUserId = currentUserIdInput.value;
    const currentUserType = currentUserTypeInput.value;
    // user name unused locally but kept fyi

    let connection = null;
    let selectedUser = null;
    let messages = {}; // Store messages by conversation key

    // Initialize SignalR connection
    function initializeSignalR() {
        try {
            connection = new signalR.HubConnectionBuilder()
                .withUrl("/privatechathub")
                .withAutomaticReconnect()
                .build();

            // Handle receiving messages
            connection.on("ReceiveMessage", function (message) {
                const conversationKey = `${message.senderType}_${message.senderId}`;

                if (!messages[conversationKey]) {
                    messages[conversationKey] = [];
                }
                messages[conversationKey].push(message);

                // If this conversation is active, display the message
                if (selectedUser &&
                    selectedUser.userId === message.senderId &&
                    selectedUser.userType === message.senderType) {
                    appendMessage(message, false);
                } else {
                    // Show unread badge
                    updateUnreadBadge(message.senderType, message.senderId, 1);
                }
            });

            // Handle sent message confirmation
            connection.on("MessageSent", function (message) {
                appendMessage(message, true);
            });

            // Handle online users update
            connection.on("UpdateOnlineUsers", function (users) {
                updateOnlineStatus(users);
            });

            // --- SignalR Events for Actions ---

            // Handle message withdrawn
            connection.on("MessagesWithdrawn", function (messageIds) {
                messageIds.forEach(id => {
                    const msgEl = document.querySelector(`.message[data-message-id="${id}"]`);
                    if (msgEl) {
                        const contentEl = msgEl.querySelector('.message-content');
                        contentEl.innerHTML = '<em><i class="bi bi-x-circle"></i> Message withdrawn</em>';
                        contentEl.classList.add('text-muted');
                        msgEl.dataset.isWithdrawn = "true";
                        // Uncheck if selected
                        const checkbox = msgEl.querySelector('.message-select');
                        if (checkbox) checkbox.checked = false;
                    }
                });
                // Refresh selection state in case one was selected
                updateActionButtons();
            });

            // Handle message deleted
            connection.on("MessagesDeleted", function (messageIds) {
                messageIds.forEach(id => {
                    const msgEl = document.querySelector(`.message[data-message-id="${id}"]`);
                    if (msgEl) {
                        msgEl.remove();
                        selectedMessages.delete(id);
                    }
                });
                updateActionButtons();
            });


            // Start connection
            startConnection();
        } catch (err) {
            console.error("Error initializing SignalR:", err);
            showConnectionStatus('disconnected');
        }
    }

    function startConnection() {
        if (!connection) return;

        showConnectionStatus('connecting');

        connection.start()
            .then(function () {
                console.log("SignalR Connected");
                showConnectionStatus('connected');

                // Get current online users
                connection.invoke("GetOnlineUsers")
                    .then(function (users) {
                        updateOnlineStatus(users);
                    })
                    .catch(err => console.error("Error getting online users:", err));
            })
            .catch(function (err) {
                console.error("SignalR Connection Error:", err);
                showConnectionStatus('disconnected');
                setTimeout(startConnection, 5000);
            });

        connection.onclose(function () {
            showConnectionStatus('disconnected');
        });

        connection.onreconnecting(function () {
            showConnectionStatus('connecting');
        });

        connection.onreconnected(function () {
            showConnectionStatus('connected');
        });
    }

    function showConnectionStatus(status) {
        let statusDiv = document.getElementById('connectionStatus');
        if (!statusDiv) {
            statusDiv = document.createElement('div');
            statusDiv.id = 'connectionStatus';
            statusDiv.className = 'connection-status';
            document.body.appendChild(statusDiv);
        }

        statusDiv.className = `connection-status ${status}`;

        if (status === 'connected') {
            statusDiv.innerHTML = '<i class="bi bi-wifi"></i> Connected';
            setTimeout(() => statusDiv.style.display = 'none', 2000);
        } else if (status === 'disconnected') {
            statusDiv.innerHTML = '<i class="bi bi-wifi-off"></i> Disconnected';
            statusDiv.style.display = 'flex';
        } else {
            statusDiv.innerHTML = '<i class="bi bi-arrow-repeat"></i> Connecting...';
            statusDiv.style.display = 'flex';
        }
    }

    function updateOnlineStatus(users) {
        // Reset all indicators
        document.querySelectorAll('.online-indicator').forEach(el => {
            el.classList.remove('online');
        });

        // Set online for connected users
        if (users && Array.isArray(users)) {
            users.forEach(function (user) {
                const indicator = document.getElementById(`status-${user.userType}-${user.userId}`);
                if (indicator) {
                    indicator.classList.add('online');
                }
            });
        }
    }

    function updateUnreadBadge(userType, userId, increment) {
        const badge = document.getElementById(`unread-${userType}-${userId}`);
        if (badge) {
            let count = parseInt(badge.textContent) || 0;
            count += increment;
            badge.textContent = count;
            badge.classList.toggle('d-none', count === 0);
        }
    }

    // --- UI State ---
    let selectionMode = false;
    let selectedMessages = new Set();


    // --- Helper Functions ---

    window.toggleSelectionMode = function () {
        selectionMode = !selectionMode;
        document.getElementById('chatMessages').classList.toggle('selection-mode', selectionMode);

        const actionsDiv = document.getElementById('chatActions');
        const selectBtn = document.getElementById('selectModeBtn');

        if (selectionMode) {
            if (actionsDiv) actionsDiv.classList.remove('d-none');
            if (selectBtn) selectBtn.classList.add('d-none');
        } else {
            if (actionsDiv) actionsDiv.classList.add('d-none');
            if (selectBtn) selectBtn.classList.remove('d-none');
            // Clear selection
            document.querySelectorAll('.message-select').forEach(cb => cb.checked = false);
            selectedMessages.clear();
            updateActionButtons();
        }
    }

    window.updateSelection = function (checkbox, id) {
        if (checkbox.checked) {
            selectedMessages.add(id);
        } else {
            selectedMessages.delete(id);
        }
        updateActionButtons();
    }

    function updateActionButtons() {
        const count = selectedMessages.size;
        const withdrawBtn = document.getElementById('btnWithdraw');
        const deleteBtn = document.getElementById('btnDelete');

        if (deleteBtn) deleteBtn.disabled = count === 0;

        if (withdrawBtn) {
            if (count === 0) {
                withdrawBtn.disabled = true;
            } else {
                let canWithdraw = true;
                const now = new Date();

                selectedMessages.forEach(id => {
                    const msgEl = document.querySelector(`.message[data-message-id="${id}"]`);
                    if (msgEl) {
                        // Check 1: Must be sent by current user
                        if (!msgEl.classList.contains('sent')) {
                            canWithdraw = false;
                        }

                        // Check 2: Must be within 2 minutes
                        const sentAtStr = msgEl.dataset.sentAt;
                        if (sentAtStr) {
                            const sentAt = new Date(sentAtStr);
                            const diffMinutes = (now - sentAt) / 1000 / 60;
                            if (diffMinutes > 2) {
                                canWithdraw = false;
                            }
                        }
                    }
                });
                withdrawBtn.disabled = !canWithdraw;
            }
        }
    }

    window.withdrawSelected = function () {
        if (selectedMessages.size === 0) return;
        if (!confirm("Withdraw selected messages? This will remove them for everyone.")) return;

        const ids = Array.from(selectedMessages);
        connection.invoke("WithdrawMessages", ids)
            .then(() => {
                toggleSelectionMode(); // Exit selection mode
            })
            .catch(err => console.error("Withdraw failed", err));
    }

    window.deleteSelected = function () {
        if (selectedMessages.size === 0) return;
        if (!confirm("Delete selected messages? This will only hide them for you.")) return;

        const ids = Array.from(selectedMessages);
        connection.invoke("DeleteMessages", ids)
            .then(() => {
                toggleSelectionMode(); // Exit selection mode
            })
            .catch(err => console.error("Delete failed", err));
    }

    function selectUser(userItem) {
        // Reset state
        selectionMode = false;
        selectedMessages.clear();
        const chatMessagesEl = document.getElementById('chatMessages');
        if (chatMessagesEl) chatMessagesEl.classList.remove('selection-mode');

        // Remove active from all
        document.querySelectorAll('.user-item').forEach(el => el.classList.remove('active'));
        userItem.classList.add('active');

        selectedUser = {
            userId: userItem.dataset.userId,
            userType: userItem.dataset.userType,
            userName: userItem.dataset.userName
        };

        // Update header with actions
        const chatHeader = document.getElementById('chatHeader');
        if (chatHeader) {
            chatHeader.innerHTML = `
                <div class="d-flex justify-content-between align-items-center w-100">
                    <div class="receiver-info">
                        <div class="receiver-avatar">
                            <i class="bi bi-person-fill"></i>
                        </div>
                        <div>
                            <div class="receiver-name">${selectedUser.userName}</div>
                            <small class="text-muted">${selectedUser.userType}</small>
                        </div>
                    </div>
                    <div class="d-flex align-items-center gap-2">
                        <div id="chatActions" class="d-none">
                            <button id="btnWithdraw" class="btn btn-sm btn-outline-warning" onclick="withdrawSelected()" disabled>
                                <i class="bi bi-arrow-return-left"></i> withdraw
                            </button>
                            <button id="btnDelete" class="btn btn-sm btn-outline-danger" onclick="deleteSelected()" disabled>
                                <i class="bi bi-trash"></i> Delete
                            </button>
                            <button class="btn btn-sm btn-link text-muted" onclick="toggleSelectionMode()">Cancel</button>
                        </div>
                        <button id="selectModeBtn" class="btn btn-sm btn-outline-secondary" onclick="toggleSelectionMode()">
                            <i class="bi bi-check2-square"></i> Select
                        </button>
                    </div>
                </div>
            `;
        }

        // Clear messages and show spinner
        const messagesDiv = document.getElementById('chatMessages');
        if (messagesDiv) {
            messagesDiv.innerHTML = '<div class="text-center p-4"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading...</span></div></div>';
        }

        // Show input
        const chatInput = document.getElementById('chatInput');
        if (chatInput) chatInput.style.display = 'block';

        const messageText = document.getElementById('messageText');
        if (messageText) messageText.focus();

        // Clear unread badge
        const badge = document.getElementById(`unread-${selectedUser.userType}-${selectedUser.userId}`);
        if (badge) {
            badge.textContent = '0';
            badge.classList.add('d-none');
        }

        // Fetch chat history from server
        // CHECK if connection is ready
        if (!connection || connection.state !== "Connected") {
            const status = connection ? connection.state : "Not Initialized";
            console.warn("SignalR not connected when selecting user. Status:", status);

            if (messagesDiv) {
                messagesDiv.innerHTML = `
                    <div class="text-center text-muted p-4">
                        <i class="bi bi-wifi-off display-6"></i>
                        <p class="mt-2">Connecting to chat service...</p>
                        <small>Status: ${status}</small>
                    </div>`;
            }

            // If disconnected, try to start
            if (connection && connection.state === "Disconnected") {
                startConnection();
            }
            return;
        }

        connection.invoke("GetChatHistory", selectedUser.userType, selectedUser.userId)
            .then(function (history) {
                if (messagesDiv) messagesDiv.innerHTML = ''; // Clear spinner

                if (history && history.length > 0) {
                    history.forEach(function (msg) {
                        const isSent = msg.senderId === currentUserId && msg.senderType === currentUserType;
                        appendMessage(msg, isSent);
                    });
                } else {
                    // Show empty state if no messages
                    if (messagesDiv) {
                        messagesDiv.innerHTML = `
                            <div class="empty-state">
                                <i class="bi bi-chat-heart display-1 text-muted"></i>
                                <h5 class="mt-3 text-muted">No messages yet</h5>
                                <p class="text-muted">Start the conversation with ${selectedUser.userName}</p>
                            </div>
                        `;
                    }
                }
            })
            .catch(function (err) {
                console.error("Error fetching history:", err);
                if (messagesDiv) messagesDiv.innerHTML = '<div class="text-center text-danger p-4">Failed to load messages</div>';
            });
    }

    function appendMessage(message, isSent) {
        const messagesDiv = document.getElementById('chatMessages');
        if (!messagesDiv) return;

        // Remove empty state if present
        const emptyState = messagesDiv.querySelector('.empty-state');
        if (emptyState) emptyState.remove();

        const messageDiv = document.createElement('div');
        messageDiv.className = `message ${isSent ? 'sent' : 'received'}`;
        messageDiv.dataset.messageId = message.id || ''; // Ensure ID is set
        messageDiv.dataset.isWithdrawn = message.isWithdrawn;
        messageDiv.dataset.sentAt = message.sentAt;

        const time = new Date(message.sentAt).toLocaleTimeString('en-US', {
            hour: 'numeric',
            minute: '2-digit',
            hour12: true
        });

        let contentHtml;
        if (message.isWithdrawn) {
            contentHtml = '<em><i class="bi bi-x-circle"></i> Message withdrawn</em>';
        } else if (message.messageType === 'Image' || message.messageType === 'Gif') {
            // Render image or GIF
            contentHtml = `<img src="${escapeHtml(message.content)}" alt="${message.messageType}" class="chat-media" onclick="openImageModal('${escapeHtml(message.content)}')"/>`;
        } else {
            contentHtml = escapeHtml(message.content);
        }

        messageDiv.innerHTML = `
            <div class="message-select-container">
                <input type="checkbox" class="message-select form-check-input" 
                       onclick="updateSelection(this, '${message.id}')">
            </div>
            <div class="message-bubble ${(message.messageType === 'Image' || message.messageType === 'Gif') ? 'media-bubble' : ''}">
                <div class="message-content ${message.isWithdrawn ? 'text-muted' : ''}">${contentHtml}</div>
                <div class="message-time">${time}</div>
            </div>
        `;

        messagesDiv.appendChild(messageDiv);
        messagesDiv.scrollTop = messagesDiv.scrollHeight;
    }

    // Image modal for full-size viewing
    window.openImageModal = function (src) {
        const modal = document.createElement('div');
        modal.className = 'image-modal';
        modal.innerHTML = `
            <div class="image-modal-content">
                <img src="${src}" alt="Full size image">
                <button class="btn btn-light btn-close-modal" onclick="this.parentElement.parentElement.remove()">&times;</button>
            </div>
        `;
        modal.onclick = function (e) {
            if (e.target === modal) modal.remove();
        };
        document.body.appendChild(modal);
    }

    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function sendMessage(text) {
        sendMediaMessage(text, 'Text');
    }

    function sendMediaMessage(content, messageType) {
        if (!selectedUser || !content || !connection) return;

        if (connection.state !== "Connected") {
            alert("Connection lost. Reconnecting...");
            return;
        }

        connection.invoke("SendMediaMessage", selectedUser.userType, selectedUser.userId, content, messageType)
            .catch(function (err) {
                console.error("Send Error:", err);
                const errorMessage = err.message ? err.message : "Failed to send message. Please try again.";
                const cleanMessage = errorMessage.replace(/^.*\bHubException: /, '');
                alert(cleanMessage);
            });
    }

    // Event Listeners
    console.log("Attaching event listeners to user items...");
    const userItems = document.querySelectorAll('.user-item');
    console.log("Found user items:", userItems.length);

    userItems.forEach(function (item) {
        item.addEventListener('click', function () {
            console.log("User item clicked:", this.dataset.userName);
            selectUser(this);
        });
    });

    const messageForm = document.getElementById('messageForm');
    if (messageForm) {
        messageForm.addEventListener('submit', function (e) {
            e.preventDefault();
            const input = document.getElementById('messageText');
            const text = input.value;
            if (text.trim()) {
                sendMessage(text);
                input.value = '';
            }
        });

        // Emoji Picker Logic
        const emojiBtn = document.getElementById('emojiBtn');
        const emojiContainer = document.getElementById('emojiPickerContainer');
        const messageInput = document.getElementById('messageText');
        let picker = null;

        if (emojiBtn && emojiContainer && typeof picmo !== 'undefined') {
            const { createPicker } = picmo; // Access createPicker from global picmo object

            picker = createPicker({
                rootElement: emojiContainer
            });

            picker.addEventListener('emoji:select', selection => {
                const start = messageInput.selectionStart;
                const end = messageInput.selectionEnd;
                const text = messageInput.value;
                const before = text.substring(0, start);
                const after = text.substring(end, text.length);

                messageInput.value = before + selection.emoji + after;
                messageInput.selectionStart = messageInput.selectionEnd = start + selection.emoji.length;
                messageInput.focus();
            });

            emojiBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                emojiContainer.classList.toggle('show');
                // Calculate position: above the button
                const btnRect = emojiBtn.getBoundingClientRect();
                // emojiContainer.style.bottom = '60px'; // Initial simple positioning
                // emojiContainer.style.left = '0';
            });

            // Close when clicking outside
            document.addEventListener('click', (e) => {
                if (!emojiContainer.contains(e.target) && e.target !== emojiBtn && !emojiBtn.contains(e.target)) {
                    emojiContainer.classList.remove('show');
                }
            });
        }

        // Voice Input Logic
        const micBtn = document.getElementById('micBtn');
        if (micBtn) {
            const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;

            if (SpeechRecognition) {
                const recognition = new SpeechRecognition();
                recognition.continuous = false;
                recognition.lang = 'en-US';
                recognition.interimResults = false;

                let isRecording = false;

                micBtn.addEventListener('click', () => {
                    if (isRecording) {
                        recognition.stop();
                    } else {
                        recognition.start();
                    }
                });

                recognition.onstart = function () {
                    isRecording = true;
                    micBtn.classList.remove('btn-light');
                    micBtn.classList.add('btn-danger', 'recording-pulse');
                    micBtn.innerHTML = '<i class="bi bi-mic-fill"></i>';
                };

                recognition.onend = function () {
                    isRecording = false;
                    micBtn.classList.remove('btn-danger', 'recording-pulse');
                    micBtn.classList.add('btn-light');
                    micBtn.innerHTML = '<i class="bi bi-mic"></i>';
                };

                recognition.onresult = function (event) {
                    const transcript = event.results[0][0].transcript;
                    const input = document.getElementById('messageText');

                    const start = input.selectionStart;
                    const end = input.selectionEnd;
                    const text = input.value;
                    const before = text.substring(0, start);
                    const after = text.substring(end, text.length);

                    // Add space if needed
                    const prefix = (start > 0 && !/\s$/.test(before)) ? ' ' : '';

                    input.value = before + prefix + transcript + after;
                    input.focus();

                    // Move cursor to end of inserted text
                    const newCursorPos = start + prefix.length + transcript.length;
                    input.selectionStart = input.selectionEnd = newCursorPos;
                };

                recognition.onerror = function (event) {
                    console.error('Speech recognition error', event.error);
                    isRecording = false;
                    micBtn.classList.remove('btn-danger', 'recording-pulse');
                    micBtn.classList.add('btn-light');
                    micBtn.innerHTML = '<i class="bi bi-mic-mute"></i>';
                    setTimeout(() => {
                        micBtn.innerHTML = '<i class="bi bi-mic"></i>';
                    }, 2000);
                };

            } else {
                micBtn.style.display = 'none';
                console.warn("Web Speech API not supported in this browser.");
            }
        }

        // Image Upload Logic
        const imageBtn = document.getElementById('imageBtn');
        const imageFileInput = document.getElementById('imageFileInput');
        const imagePreview = document.getElementById('imagePreview');
        const previewImage = document.getElementById('previewImage');
        const cancelImage = document.getElementById('cancelImage');
        let pendingImageFile = null;

        if (imageBtn && imageFileInput) {
            imageBtn.addEventListener('click', () => {
                imageFileInput.click();
            });

            imageFileInput.addEventListener('change', (e) => {
                const file = e.target.files[0];
                if (file) {
                    // Validate file size (5MB max)
                    if (file.size > 5 * 1024 * 1024) {
                        alert('Image size must be less than 5MB');
                        imageFileInput.value = '';
                        return;
                    }

                    pendingImageFile = file;
                    const reader = new FileReader();
                    reader.onload = (ev) => {
                        previewImage.src = ev.target.result;
                        imagePreview.classList.remove('d-none');
                    };
                    reader.readAsDataURL(file);
                }
            });

            if (cancelImage) {
                cancelImage.addEventListener('click', () => {
                    pendingImageFile = null;
                    imageFileInput.value = '';
                    imagePreview.classList.add('d-none');
                    previewImage.src = '';
                });
            }

            // Modify form submit to handle pending image
            messageForm.addEventListener('submit', async function (e) {
                if (pendingImageFile) {
                    e.preventDefault();

                    const formData = new FormData();
                    formData.append('file', pendingImageFile);

                    try {
                        const response = await fetch('/PrivateChat/UploadImage', {
                            method: 'POST',
                            body: formData
                        });

                        if (response.ok) {
                            const data = await response.json();
                            sendMediaMessage(data.url, 'Image');

                            // Clear preview
                            pendingImageFile = null;
                            imageFileInput.value = '';
                            imagePreview.classList.add('d-none');
                            previewImage.src = '';
                        } else {
                            const error = await response.json();
                            alert(error.error || 'Failed to upload image');
                        }
                    } catch (err) {
                        console.error('Upload error:', err);
                        alert('Failed to upload image');
                    }
                }
            }, true); // Use capture phase
        }

        // GIF Picker Logic
        const gifBtn = document.getElementById('gifBtn');
        const gifPickerContainer = document.getElementById('gifPickerContainer');
        const gifSearchInput = document.getElementById('gifSearchInput');
        const gifPickerContent = document.getElementById('gifPickerContent');
        const gifPickerClose = document.getElementById('gifPickerClose');
        const GIPHY_API_KEY = 'GlVGYHkr3WSBnllca54iNt0yFbjz7L65'; // Public beta key
        let gifSearchTimeout = null;

        if (gifBtn && gifPickerContainer) {
            gifBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                gifPickerContainer.classList.toggle('show');
                emojiContainer.classList.remove('show'); // Close emoji picker

                // Load trending GIFs when opened
                if (gifPickerContainer.classList.contains('show')) {
                    loadTrendingGifs();
                }
            });

            if (gifPickerClose) {
                gifPickerClose.addEventListener('click', () => {
                    gifPickerContainer.classList.remove('show');
                });
            }

            if (gifSearchInput) {
                gifSearchInput.addEventListener('input', () => {
                    clearTimeout(gifSearchTimeout);
                    gifSearchTimeout = setTimeout(() => {
                        const query = gifSearchInput.value.trim();
                        if (query) {
                            searchGifs(query);
                        } else {
                            loadTrendingGifs();
                        }
                    }, 500);
                });
            }

            document.addEventListener('click', (e) => {
                if (!gifPickerContainer.contains(e.target) && e.target !== gifBtn && !gifBtn.contains(e.target)) {
                    gifPickerContainer.classList.remove('show');
                }
            });
        }

        async function loadTrendingGifs() {
            try {
                gifPickerContent.innerHTML = '<div class="text-center p-3"><div class="spinner-border spinner-border-sm" role="status"></div></div>';
                const response = await fetch(`https://api.giphy.com/v1/gifs/trending?api_key=${GIPHY_API_KEY}&limit=20&rating=g`);
                const data = await response.json();
                renderGifs(data.data);
            } catch (err) {
                console.error('Error loading trending GIFs:', err);
                gifPickerContent.innerHTML = '<div class="text-center text-danger p-3">Failed to load GIFs</div>';
            }
        }

        async function searchGifs(query) {
            try {
                gifPickerContent.innerHTML = '<div class="text-center p-3"><div class="spinner-border spinner-border-sm" role="status"></div></div>';
                const response = await fetch(`https://api.giphy.com/v1/gifs/search?api_key=${GIPHY_API_KEY}&q=${encodeURIComponent(query)}&limit=20&rating=g`);
                const data = await response.json();
                renderGifs(data.data);
            } catch (err) {
                console.error('Error searching GIFs:', err);
                gifPickerContent.innerHTML = '<div class="text-center text-danger p-3">Failed to search GIFs</div>';
            }
        }

        function renderGifs(gifs) {
            if (!gifs || gifs.length === 0) {
                gifPickerContent.innerHTML = '<div class="text-center text-muted p-3">No GIFs found</div>';
                return;
            }

            const html = gifs.map(gif => `
                <div class="gif-item" onclick="selectGif('${gif.images.fixed_height.url}')">
                    <img src="${gif.images.fixed_height_small.url}" alt="${escapeHtml(gif.title)}" loading="lazy">
                </div>
            `).join('');

            gifPickerContent.innerHTML = `<div class="gif-grid">${html}</div>`;
        }

        window.selectGif = function (url) {
            sendMediaMessage(url, 'Gif');
            gifPickerContainer.classList.remove('show');
            gifSearchInput.value = '';
        };
    }

    // User search
    const userSearch = document.getElementById('userSearch');
    if (userSearch) {
        userSearch.addEventListener('input', function () {
            const query = this.value.toLowerCase();
            document.querySelectorAll('.user-item').forEach(function (item) {
                const name = item.dataset.userName.toLowerCase();
                item.style.display = name.includes(query) ? 'flex' : 'none';
            });
        });
    }

    // Initialize
    initializeSignalR();
});

document.addEventListener('DOMContentLoaded', function () {
    const chatButton = document.getElementById('chatbot-button');
    const chatWindow = document.getElementById('chat-window');
    const closeButton = document.getElementById('chat-close-btn');
    const chatInput = document.getElementById('chat-input');
    const sendButton = document.getElementById('chat-send-btn');
    const messagesContainer = document.getElementById('chat-messages');

    // Toggle chat window
    chatButton.addEventListener('click', () => {
        if (chatWindow.style.display === 'none' || chatWindow.style.display === '') {
            chatWindow.style.display = 'flex';
        } else {
            chatWindow.style.display = 'none';
        }
    });

    closeButton.addEventListener('click', () => {
        chatWindow.style.display = 'none';
    });

    // Send message
    function sendMessage() {
        const message = chatInput.value.trim();
        if (message) {
            appendMessage(message, 'user');
            chatInput.value = '';

            // Call backend API
            fetch('/api/chat/send', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ message: message })
            })
                .then(response => response.json())
                .then(data => {
                    appendMessage(data.response, 'bot');
                    loadSuggestedQuestions();
                })
                .catch(error => {
                    console.error('Error:', error);
                    appendMessage('Sorry, something went wrong. Please try again later.', 'bot');
                });
        }
    }

    sendButton.addEventListener('click', sendMessage);

    chatInput.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') {
            sendMessage();
        }
    });

    function appendMessage(text, sender) {
        const messageDiv = document.createElement('div');
        messageDiv.classList.add('message');
        messageDiv.classList.add(sender === 'user' ? 'user-message' : 'bot-message');
        messageDiv.textContent = text;
        messagesContainer.appendChild(messageDiv);
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    // Handle suggested questions - Comprehensive list covering all system features
    const allQuestions = [
        // Account & Authentication
        { label: "How do I register?", question: "How do I create an account?" },
        { label: "How to login?", question: "How do I login to my account?" },
        { label: "Forgot password?", question: "How do I reset my password?" },
        { label: "Login with Google?", question: "Can I login with Google?" },
        { label: "Update my profile?", question: "How do I update my profile?" },
        { label: "Email verification?", question: "Why do I need to verify my email?" },

        // Complaints
        { label: "Submit a complaint", question: "How do I submit a complaint?" },
        { label: "Check my status", question: "How can I check my complaint status?" },
        { label: "Submit anonymously?", question: "Can I submit a complaint anonymously?" },
        { label: "Upload attachments?", question: "How do I add attachments to my complaint?" },
        { label: "Edit my complaint?", question: "Can I edit my complaint after submitting?" },
        { label: "What are priorities?", question: "What do complaint priorities mean?" },
        { label: "Complaint statuses?", question: "What do the different complaint statuses mean?" },
        { label: "How long to resolve?", question: "How long does it take to resolve a complaint?" },

        // Notifications & Communication
        { label: "Get notifications?", question: "How do I get notifications about my complaints?" },
        { label: "Chat with staff?", question: "How can I chat with staff about my complaint?" },

        // Privacy & Security
        { label: "Is my data safe?", question: "Is my data secure?" },
        { label: "Who sees my complaint?", question: "Who can see my complaint?" },
        { label: "Privacy policy?", question: "Where can I read the privacy policy?" },

        // Roles & Features
        { label: "What can staff do?", question: "What can staff members do?" },
        { label: "What can admin do?", question: "What can administrators do?" },

        // Feedback & Help
        { label: "Give feedback?", question: "How do I give feedback on a resolved complaint?" },
        { label: "Contact support?", question: "How do I contact support?" },
        { label: "View FAQs?", question: "Where can I find FAQs?" }
    ];

    function loadSuggestedQuestions() {
        let container = document.getElementById('suggested-questions');
        if (container) {
            container.remove();
        }

        container = document.createElement('div');
        container.id = 'suggested-questions';
        container.classList.add('suggested-questions');

        // Shuffle and pick 4
        const shuffled = allQuestions.sort(() => 0.5 - Math.random());
        const selected = shuffled.slice(0, 4);

        selected.forEach(q => {
            const btn = document.createElement('button');
            btn.classList.add('question-btn');
            btn.textContent = q.label;
            btn.setAttribute('data-question', q.question);

            btn.addEventListener('click', () => {
                chatInput.value = q.question;
                sendMessage();
            });

            container.appendChild(btn);
        });

        messagesContainer.appendChild(container);
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    loadSuggestedQuestions();
});

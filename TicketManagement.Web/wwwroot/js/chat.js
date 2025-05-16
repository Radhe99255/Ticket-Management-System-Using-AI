/**
 * Unified Chat Module - Handles chat functionality for both users and admins
 */
(function() {
    // Configuration
    let config = {
        ticketId: 0,
        userId: 0,
        isAdmin: false,
        serverUrl: '',
        retryAttempts: 0,
        maxRetryAttempts: 5
    };
    
    // State
    let state = {
        connection: null,
        isConnected: false,
        messageQueue: []
    };
    
    // DOM Elements
    let elements = {
        chatContainer: null,
        messageForm: null,
        messageInput: null,
        sendButton: null,
        noMessagesElement: null,
        statusBadge: null
    };
    
    /**
     * Initialize the chat module
     * @param {Object} options - Configuration options 
     */
    function init(options) {
        // Apply configuration
        config.ticketId = parseInt(options.ticketId, 10);
        config.userId = parseInt(options.userId, 10);
        config.isAdmin = !!options.isAdmin;
        config.serverUrl = options.serverUrl || window.location.origin;
        
        // Cache DOM elements
        elements.chatContainer = document.getElementById('chat-messages');
        elements.messageForm = document.getElementById('message-form');
        elements.messageInput = document.getElementById('message-content');
        elements.sendButton = document.querySelector('#message-form button[type="submit"]');
        elements.noMessagesElement = document.getElementById('no-messages');
        elements.statusBadge = document.getElementById('connection-status-badge');
        
        console.log(`Initializing chat for ticket #${config.ticketId}`);
        console.log(`User ID: ${config.userId}, isAdmin: ${config.isAdmin}`);
        
        // Add WhatsApp-like styles
        addWhatsAppStyles();
        
        // Start SignalR connection
        startConnection();
        
        // Set up event handlers
        setupEventHandlers();
        
        // Fetch initial messages as fallback
        fetchMessages();
        
        // Auto-scroll to bottom
        scrollToBottom();
    }
    
    /**
     * Start the SignalR connection
     */
    function startConnection() {
        updateConnectionStatus('connecting');
        
        // Stop any existing connection
        if (state.connection) {
            try {
                state.connection.stop();
                console.log('Stopped previous connection');
            } catch (err) {
                console.error('Error stopping previous connection:', err);
            }
        }
        
        // Create hub URL
        const hubUrl = `${config.serverUrl}/chathub`;
        console.log(`Connecting to chat hub at: ${hubUrl}`);
        
        // Create connection with appropriate options
        state.connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl, {
                skipNegotiation: false,
                transport: signalR.HttpTransportType.WebSockets | 
                           signalR.HttpTransportType.ServerSentEvents | 
                           signalR.HttpTransportType.LongPolling
            })
            .withAutomaticReconnect([0, 1000, 3000, 5000, 10000, 15000])
            .configureLogging(signalR.LogLevel.Information)
            .build();
        
        // Set up SignalR event handlers
        setupSignalRHandlers();
        
        // Connect to the hub
        state.connection.start()
            .then(() => {
                console.log('✓ Connected to SignalR hub');
                state.isConnected = true;
                config.retryAttempts = 0;
                
                // Update UI with connection info
                const transportType = state.connection.connection.transport.name;
                updateConnectionStatus('connected', transportType);
                
                // Join the ticket's chat group
                return state.connection.invoke('JoinTicketGroup', config.ticketId);
            })
            .then(() => {
                console.log(`✓ Joined ticket-${config.ticketId} group`);
                
                // Process any queued messages
                processMessageQueue();
            })
            .catch(err => {
                console.error('Error connecting to SignalR hub:', err);
                state.isConnected = false;
                updateConnectionStatus('failed');
                
                // Retry connection with exponential backoff
                if (config.retryAttempts < config.maxRetryAttempts) {
                    const delay = Math.min(1000 * Math.pow(2, config.retryAttempts), 30000);
                    console.log(`Will retry connection in ${delay}ms`);
                    config.retryAttempts++;
                    setTimeout(startConnection, delay);
                } else {
                    console.warn('Max retry attempts reached. Using polling fallback.');
                    startPolling();
                }
            });
    }
    
    /**
     * Set up the SignalR event handlers
     */
    function setupSignalRHandlers() {
        // Handle reconnecting
        state.connection.onreconnecting(error => {
            console.warn('Connection lost, reconnecting...', error);
            state.isConnected = false;
            updateConnectionStatus('reconnecting');
        });
        
        // Handle successful reconnection
        state.connection.onreconnected(connectionId => {
            console.log('Connection reestablished. ID:', connectionId);
            state.isConnected = true;
            updateConnectionStatus('connected');
            
            // Rejoin the ticket group and refresh messages
            state.connection.invoke('JoinTicketGroup', config.ticketId)
                .then(() => {
                    console.log(`Rejoined ticket-${config.ticketId} group`);
                    fetchMessages();
                    processMessageQueue();
                })
                .catch(err => console.error('Error rejoining group:', err));
        });
        
        // Handle disconnection
        state.connection.onclose(error => {
            console.warn('Connection closed:', error);
            state.isConnected = false;
            updateConnectionStatus('disconnected');
            
            // Try to reconnect if appropriate
            if (config.retryAttempts < config.maxRetryAttempts) {
                const delay = Math.min(1000 * Math.pow(2, config.retryAttempts), 30000);
                console.log(`Will attempt to reconnect in ${delay}ms`);
                config.retryAttempts++;
                setTimeout(startConnection, delay);
            }
        });
        
        // Handle receiving messages
        state.connection.on('ReceiveMessage', message => {
            console.log('Received message from server:', message);
            
            // Display the message if it's not already shown
            if (!isMessageAlreadyDisplayed(message)) {
                addMessageToChat(message);
            } else {
                console.log('Message already displayed, skipping');
            }
        });
        
        // Handle group joined confirmation
        state.connection.on('JoinedGroup', ticketId => {
            console.log(`Successfully joined ticket-${ticketId} group`);
        });
        
        // Handle connection check response
        state.connection.on('Pong', data => {
            console.log('Received pong from server:', data.timestamp);
        });
    }
    
    /**
     * Check if a message is already displayed in the chat
     */
    function isMessageAlreadyDisplayed(message) {
        // Normalize message ID
        const msgId = message.id || message.Id || message.messageId || message.MessageId || 0;
        
        // If no valid ID, can't check
        if (!msgId) return false;
        
        // Check by message ID
        const existingMsg = document.getElementById(`message-${msgId}`);
        return !!existingMsg;
    }
    
    /**
     * Add a message to the chat display
     */
    function addMessageToChat(message) {
        if (!message) {
            console.warn('Invalid message object');
            return;
        }
        
        // Hide the "no messages" element if present
        if (elements.noMessagesElement) {
            elements.noMessagesElement.style.display = 'none';
        }
        
        // Normalize message properties to handle different casing from server
        const normalizedMsg = {
            id: message.id || message.Id || message.messageId || message.MessageId,
            ticketId: message.ticketId || message.TicketId || config.ticketId,
            senderId: message.senderId || message.SenderId,
            senderName: message.senderName || message.SenderName || 'Unknown',
            content: message.content || message.Content || '',
            isAdmin: message.isAdmin || message.IsAdmin || message.isSenderAdmin || message.IsSenderAdmin || false,
            timestamp: message.timestamp || message.Timestamp || message.sentAt || message.SentAt || new Date(),
            isRead: message.isRead || message.IsRead || false
        };
        
        // Create a safe ID for the message element
        const messageId = normalizedMsg.id.toString();
        
        // Determine if this is the current user's message
        const isCurrentUser = parseInt(normalizedMsg.senderId) === config.userId;
        
        // Format sender name
        let senderName = normalizedMsg.senderName;
        if (isCurrentUser) {
            senderName = 'You';
        } else if (normalizedMsg.isAdmin && !senderName.includes('(Admin)')) {
            senderName += ' (Admin)';
        }
        
        // Format timestamp
        const timestamp = new Date(normalizedMsg.timestamp).toLocaleString([], {hour: '2-digit', minute:'2-digit'});
        
        // Create message element with WhatsApp-like styling
        const messageElement = document.createElement('div');
        messageElement.id = `message-${messageId}`;
        messageElement.className = `message ${isCurrentUser ? 'message-sent' : 'message-received'} mb-2`;
        messageElement.setAttribute('data-sender-id', normalizedMsg.senderId);
        messageElement.setAttribute('data-timestamp', normalizedMsg.timestamp);
        
        // WhatsApp-like message bubbles with tails
        const bubbleClass = isCurrentUser ? 'whatsapp-bubble-sent' : 'whatsapp-bubble-received';
        const alignClass = isCurrentUser ? 'align-items-end' : 'align-items-start';
        
        messageElement.innerHTML = `
            <div class="d-flex flex-column ${alignClass} w-100">
                ${!isCurrentUser ? `<div class="sender-name small mb-1">${senderName}</div>` : ''}
                <div class="message-content ${bubbleClass}">
                    <div class="message-text">${normalizedMsg.content}</div>
                    <div class="message-time small">${timestamp}</div>
                </div>
            </div>
        `;
        
        // Add to messages container
        let messagesContainer = document.querySelector('.messages');
        if (!messagesContainer) {
            messagesContainer = document.createElement('div');
            messagesContainer.className = 'messages';
            elements.chatContainer.appendChild(messagesContainer);
        }
        
        messagesContainer.appendChild(messageElement);
        
        // Scroll to the bottom
        scrollToBottom();
        
        // Mark message as read if it's not from the current user
        if (!isCurrentUser && !normalizedMsg.isRead) {
            markMessageAsRead(normalizedMsg.id);
        }
    }
    
    /**
     * Send a new chat message
     */
    async function sendMessage(content) {
        if (!content.trim()) {
            return;
        }
        
        try {
            // Create message to send
            const messageToSend = {
                Id: 0, // Will be assigned by the server
                TicketId: config.ticketId,
                SenderId: config.userId,
                SenderName: 'You', // Will be replaced by the server
                Content: content,
                IsAdmin: config.isAdmin,
                Timestamp: new Date(),
                IsRead: false
            };
            
            // If connected, send via SignalR
            if (state.isConnected) {
                try {
                    // Send message and let the server broadcast it back
                    await state.connection.invoke('SendMessage', messageToSend);
                    console.log('Message sent via SignalR');
                } catch (err) {
                    console.warn('Error sending via SignalR, falling back to API:', err);
                    await sendViaApi(messageToSend);
                }
            } else {
                // If not connected, send via API
                await sendViaApi(messageToSend);
                
                // Queue the message for SignalR when reconnected
                state.messageQueue.push(messageToSend);
                console.log('Connection not available, message sent via API and queued for SignalR');
            }
            
            // Clear the input field
            if (elements.messageInput) {
                elements.messageInput.value = '';
                elements.messageInput.focus();
            }
        } catch (error) {
            console.error('Error sending message:', error);
            alert('Failed to send message. Please try again.');
        }
    }
    
    /**
     * Send a message via the API as fallback
     */
    async function sendViaApi(message) {
        try {
            const endpoint = config.isAdmin ? '/Admin/SendMessage' : '/User/SendMessage';
            
            const response = await fetch(endpoint, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    TicketId: message.TicketId,
                    Content: message.Content
                })
            });
            
            const result = await response.json();
            
            if (result.success) {
                console.log('Message sent via API successfully');
                return true;
            } else {
                console.error('API message send failed:', result.message);
                return false;
            }
        } catch (err) {
            console.error('Error sending message via API:', err);
            return false;
        }
    }
    
    /**
     * Process any queued messages when connection is established
     */
    function processMessageQueue() {
        if (state.messageQueue.length > 0 && state.isConnected) {
            console.log(`Processing ${state.messageQueue.length} queued messages`);
            
            // Create a copy and clear the queue to avoid duplicate sends
            const queueCopy = [...state.messageQueue];
            state.messageQueue = [];
            
            queueCopy.forEach(async (msg) => {
                try {
                    await state.connection.invoke('SendMessage', msg);
                    console.log('Queued message sent');
                } catch (err) {
                    console.error('Failed to send queued message:', err);
                    // Add back to queue
                    state.messageQueue.push(msg);
                }
            });
        }
    }
    
    /**
     * Mark a message as read
     */
    async function markMessageAsRead(messageId) {
        if (state.isConnected) {
            try {
                await state.connection.invoke('MarkMessageAsRead', messageId);
                console.log(`Message ${messageId} marked as read`);
            } catch (err) {
                console.warn(`Error marking message ${messageId} as read:`, err);
            }
        }
    }
    
    /**
     * Fetch messages from the server
     */
    async function fetchMessages() {
        try {
            const endpoint = config.isAdmin 
                ? `/Admin/GetTicketMessages/${config.ticketId}` 
                : `/User/GetTicketMessages/${config.ticketId}`;
            
            const response = await fetch(endpoint);
            
            if (!response.ok) {
                throw new Error(`Server returned ${response.status}`);
            }
            
            const messages = await response.json();
            console.log(`Fetched ${messages.length} messages from server`);
            
            if (messages && messages.length > 0) {
                // Get the highest message ID currently displayed
                const displayedMessages = getDisplayedMessageIds();
                
                // Filter to only add new messages
                const newMessages = messages.filter(msg => {
                    const msgId = msg.messageId || msg.MessageId || 0;
                    return !displayedMessages.includes(msgId);
                });
                
                if (newMessages.length > 0) {
                    console.log(`Adding ${newMessages.length} new messages`);
                    
                    // Add each message
                    newMessages.forEach(msg => addMessageToChat(msg));
                } else {
                    console.log('No new messages to add');
                }
            } else if (!document.querySelector('.message')) {
                // Show no messages element if applicable
                if (elements.noMessagesElement) {
                    elements.noMessagesElement.style.display = 'block';
                }
            }
        } catch (error) {
            console.error('Error fetching messages:', error);
        }
    }
    
    /**
     * Get array of all message IDs currently displayed in the chat
     */
    function getDisplayedMessageIds() {
        const messages = document.querySelectorAll('.message[id^="message-"]');
        const messageIds = [];
        
        messages.forEach(msg => {
            const idParts = msg.id.split('-');
            if (idParts.length > 1) {
                const msgId = parseInt(idParts[1]);
                if (!isNaN(msgId)) {
                    messageIds.push(msgId);
                }
            }
        });
        
        return messageIds;
    }
    
    /**
     * Start polling for messages as a fallback
     */
    function startPolling() {
        console.log('Starting message polling fallback');
        
        // Clear any existing polling interval
        if (window.chatPollingInterval) {
            clearInterval(window.chatPollingInterval);
        }
        
        // Poll for new messages every 5 seconds
        window.chatPollingInterval = setInterval(fetchMessages, 5000);
    }
    
    /**
     * Update the connection status indicator
     */
    function updateConnectionStatus(status, transportType) {
        if (!elements.statusBadge) return;
        
        switch (status) {
            case 'connected':
                elements.statusBadge.className = 'badge bg-success';
                elements.statusBadge.textContent = 'Connected';
                elements.statusBadge.title = transportType 
                    ? `Connected via ${transportType}` 
                    : 'Connected to chat server';
                break;
                
            case 'connecting':
                elements.statusBadge.className = 'badge bg-secondary';
                elements.statusBadge.textContent = 'Connecting...';
                elements.statusBadge.title = 'Establishing connection to chat server';
                break;
                
            case 'reconnecting':
                elements.statusBadge.className = 'badge bg-warning';
                elements.statusBadge.textContent = 'Reconnecting...';
                elements.statusBadge.title = 'Attempting to reconnect to chat server';
                break;
                
            case 'disconnected':
                elements.statusBadge.className = 'badge bg-danger';
                elements.statusBadge.textContent = 'Disconnected';
                elements.statusBadge.title = 'Disconnected from chat server';
                break;
                
            case 'failed':
                elements.statusBadge.className = 'badge bg-danger';
                elements.statusBadge.textContent = 'Connection Failed';
                elements.statusBadge.title = 'Failed to connect to chat server';
                break;
        }
        
        console.log(`Connection status: ${status}`);
    }
    
    /**
     * Set up event handlers for the chat UI
     */
    function setupEventHandlers() {
        // Handle message form submission
        if (elements.messageForm) {
            elements.messageForm.addEventListener('submit', e => {
                e.preventDefault();
                
                const content = elements.messageInput.value.trim();
                if (content) {
                    sendMessage(content);
                    elements.messageInput.value = '';
                    elements.messageInput.focus();
                }
            });
        }
        
        // Handle connection status click to retry connection
        if (elements.statusBadge) {
            elements.statusBadge.style.cursor = 'pointer';
            elements.statusBadge.addEventListener('click', () => {
                console.log('Manual connection check requested');
                
                if (!state.isConnected) {
                    console.log('Connection is down, attempting to restart');
                    startConnection();
                } else {
                    console.log('Testing active connection with ping');
                    state.connection.invoke('Ping')
                        .then(() => console.log('Connection test successful'))
                        .catch(err => {
                            console.error('Connection test failed:', err);
                            state.isConnected = false;
                            updateConnectionStatus('failed');
                            startConnection();
                        });
                }
            });
        }
        
        // Clean up on page unload
        window.addEventListener('beforeunload', () => {
            // Stop polling if active
            if (window.chatPollingInterval) {
                clearInterval(window.chatPollingInterval);
            }
            
            // Leave the chat group
            if (state.isConnected && state.connection) {
                try {
                    state.connection.invoke('LeaveTicketGroup', config.ticketId);
                } catch (err) {
                    console.error('Error leaving chat group:', err);
                }
            }
        });
    }
    
    /**
     * Scroll the chat container to the bottom
     */
    function scrollToBottom() {
        if (elements.chatContainer) {
            elements.chatContainer.scrollTop = elements.chatContainer.scrollHeight;
        }
    }
    
    /**
     * Add WhatsApp-like styles to the chat
     */
    function addWhatsAppStyles() {
        // Add theme toggle button
        const toggleContainer = document.querySelector('.theme-toggle');
        if (toggleContainer) {
            toggleContainer.innerHTML = `
                <button id="theme-toggle-btn" class="btn btn-sm btn-light" title="Toggle Dark/Light Mode">
                    <i class="fas fa-moon"></i>
                </button>
            `;
            
            // Add event listener after the button is in the DOM
            setTimeout(() => {
                const toggleBtn = document.getElementById('theme-toggle-btn');
                if (toggleBtn) {
                    toggleBtn.addEventListener('click', toggleChatTheme);
                }
            }, 0);
        }
        
        // Add base styles
        const styleElement = document.createElement('style');
        styleElement.id = 'chat-theme-styles';
        styleElement.textContent = getLightThemeStyles();
        document.head.appendChild(styleElement);
    }
    
    /**
     * Toggle between light and dark chat themes
     */
    function toggleChatTheme() {
        const styleElement = document.getElementById('chat-theme-styles');
        const toggleBtn = document.getElementById('theme-toggle-btn');
        
        if (!toggleBtn) return;
        
        const iconElement = toggleBtn.querySelector('i');
        
        if (!iconElement) {
            // Fallback if icon element not found
            const isDarkMode = toggleBtn.classList.contains('btn-dark');
            
            if (isDarkMode) {
                // Switch to light theme
                styleElement.textContent = getLightThemeStyles();
                toggleBtn.innerHTML = '<i class="fas fa-moon"></i>';
                toggleBtn.classList.remove('btn-dark');
                toggleBtn.classList.add('btn-light');
                document.dispatchEvent(new CustomEvent('theme-changed', { detail: { isDarkTheme: false } }));
            } else {
                // Switch to dark theme
                styleElement.textContent = getDarkThemeStyles();
                toggleBtn.innerHTML = '<i class="fas fa-sun"></i>';
                toggleBtn.classList.remove('btn-light');
                toggleBtn.classList.add('btn-dark');
                document.dispatchEvent(new CustomEvent('theme-changed', { detail: { isDarkTheme: true } }));
            }
            return;
        }
        
        const isDarkTheme = iconElement.classList.contains('fa-sun');
        
        if (isDarkTheme) {
            // Switch to light theme
            styleElement.textContent = getLightThemeStyles();
            toggleBtn.innerHTML = '<i class="fas fa-moon"></i>';
            toggleBtn.classList.remove('btn-dark');
            toggleBtn.classList.add('btn-light');
            document.dispatchEvent(new CustomEvent('theme-changed', { detail: { isDarkTheme: false } }));
        } else {
            // Switch to dark theme
            styleElement.textContent = getDarkThemeStyles();
            toggleBtn.innerHTML = '<i class="fas fa-sun"></i>';
            toggleBtn.classList.remove('btn-light');
            toggleBtn.classList.add('btn-dark');
            document.dispatchEvent(new CustomEvent('theme-changed', { detail: { isDarkTheme: true } }));
        }
    }
    
    /**
     * Get light theme CSS styles
     */
    function getLightThemeStyles() {
        return `
            #chat-messages {
                background-color: #e5ddd5;
                background-image: url("data:image/png;base64,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");
                padding: 10px;
                border-radius: 5px;
                overflow-y: auto;
                color: #333;
            }
            
            .message {
                margin-bottom: 10px;
                max-width: 85%;
                clear: both;
            }
            
            .message-sent {
                float: right;
                margin-left: auto;
            }
            
            .message-received {
                float: left;
                margin-right: auto;
            }
            
            .whatsapp-bubble-sent {
                background-color: #dcf8c6;
                border-radius: 10px;
                padding: 8px 12px;
                position: relative;
                box-shadow: 0 1px 0.5px rgba(0,0,0,0.13);
                display: inline-block;
                word-break: break-word;
            }
            
            .whatsapp-bubble-received {
                background-color: white;
                border-radius: 10px;
                padding: 8px 12px;
                position: relative;
                box-shadow: 0 1px 0.5px rgba(0,0,0,0.13);
                display: inline-block;
                word-break: break-word;
            }
            
            /* WhatsApp-like tails for bubbles */
            .whatsapp-bubble-sent:before {
                content: "";
                position: absolute;
                right: -8px;
                top: 6px;
                border: 8px solid transparent;
                border-left: 8px solid #dcf8c6;
                border-top: 8px solid #dcf8c6;
                border-radius: 3px;
            }
            
            .whatsapp-bubble-received:before {
                content: "";
                position: absolute;
                left: -8px;
                top: 6px;
                border: 8px solid transparent;
                border-right: 8px solid white;
                border-top: 8px solid white;
                border-radius: 3px;
            }
            
            .message-time {
                float: right;
                margin-left: 6px;
                margin-top: 2px;
                color: #777;
                font-size: 0.7rem;
            }
            
            .message-text {
                display: inline;
            }
            
            .sender-name {
                color: #1f7aec;
                font-weight: 500;
                margin-left: 5px;
            }
            
            #message-form {
                background-color: #f0f0f0;
                padding: 10px;
                border-radius: 0 0 5px 5px;
                display: flex;
                align-items: center;
                position: relative;
            }
            
            #message-content {
                border-radius: 20px;
                padding: 10px 15px;
                flex-grow: 1;
                margin-right: 8px;
                border: none;
            }
            
            .emoji-button {
                background: none;
                border: none;
                color: #128C7E;
                font-size: 1.2rem;
                cursor: pointer;
                padding: 5px 10px;
            }
            
            #message-form button[type="submit"] {
                border-radius: 50%;
                width: 40px;
                height: 40px;
                background-color: #128C7E;
                border: none;
                display: flex;
                align-items: center;
                justify-content: center;
            }
            
            #message-form button[type="submit"] i {
                color: white;
            }
            
            .theme-toggle {
                margin-right: 10px;
            }
        `;
    }
    
    /**
     * Get dark theme CSS styles
     */
    function getDarkThemeStyles() {
        return `
            #chat-messages {
                background-color: #1f2c34;
                background-image: none;
                padding: 10px;
                border-radius: 5px;
                overflow-y: auto;
                color: #e4e6eb;
            }
            
            .message {
                margin-bottom: 10px;
                max-width: 85%;
                clear: both;
            }
            
            .message-sent {
                float: right;
                margin-left: auto;
            }
            
            .message-received {
                float: left;
                margin-right: auto;
            }
            
            .whatsapp-bubble-sent {
                background-color: #005c4b;
                border-radius: 10px;
                padding: 8px 12px;
                position: relative;
                box-shadow: 0 1px 0.5px rgba(0,0,0,0.13);
                display: inline-block;
                word-break: break-word;
            }
            
            .whatsapp-bubble-received {
                background-color: #1e2a30;
                border-radius: 10px;
                padding: 8px 12px;
                position: relative;
                box-shadow: 0 1px 0.5px rgba(0,0,0,0.13);
                display: inline-block;
                word-break: break-word;
            }
            
            /* WhatsApp-like tails for bubbles */
            .whatsapp-bubble-sent:before {
                content: "";
                position: absolute;
                right: -8px;
                top: 6px;
                border: 8px solid transparent;
                border-left: 8px solid #005c4b;
                border-top: 8px solid #005c4b;
                border-radius: 3px;
            }
            
            .whatsapp-bubble-received:before {
                content: "";
                position: absolute;
                left: -8px;
                top: 6px;
                border: 8px solid transparent;
                border-right: 8px solid #1e2a30;
                border-top: 8px solid #1e2a30;
                border-radius: 3px;
            }
            
            .message-time {
                float: right;
                margin-left: 6px;
                margin-top: 2px;
                color: #aaa;
                font-size: 0.7rem;
            }
            
            .message-text {
                display: inline;
            }
            
            .sender-name {
                color: #53bdeb;
                font-weight: 500;
                margin-left: 5px;
            }
            
            #message-form {
                background-color: #1f2c34;
                padding: 10px;
                border-radius: 0 0 5px 5px;
                display: flex;
                align-items: center;
                position: relative;
            }
            
            #message-content {
                background-color: #2a3942;
                color: #e4e6eb;
                border-radius: 20px;
                padding: 10px 15px;
                flex-grow: 1;
                margin-right: 8px;
                border: none;
            }
            
            #message-content::placeholder {
                color: #8696a0;
            }
            
            .emoji-button {
                background: none;
                border: none;
                color: #00a884;
                font-size: 1.2rem;
                cursor: pointer;
                padding: 5px 10px;
            }
            
            #message-form button[type="submit"] {
                border-radius: 50%;
                width: 40px;
                height: 40px;
                background-color: #00a884;
                border: none;
                display: flex;
                align-items: center;
                justify-content: center;
            }
            
            #message-form button[type="submit"] i {
                color: white;
            }
            
            .theme-toggle {
                margin-right: 10px;
            }
        `;
    }
    
    // Public API
    window.Chat = {
        init: init
    };
})(); 
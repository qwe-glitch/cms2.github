using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace ComplaintManagementSystem.Hubs
{
    public class PrivateChatHub : Hub
    {
        // Track connected users: Key = "UserType_UserId", Value = ConnectionId
        private static readonly ConcurrentDictionary<string, string> ConnectedUsers = new();
        
        // Track online user info for display
        private static readonly ConcurrentDictionary<string, UserInfo> OnlineUsers = new();

        private readonly ComplaintManagementSystem.Services.ChatStorageService _chatStorage;
        private readonly ComplaintManagementSystem.Services.ISpamDetectionService _spamDetection;

        public PrivateChatHub(
            ComplaintManagementSystem.Services.ChatStorageService chatStorage,
            ComplaintManagementSystem.Services.ISpamDetectionService spamDetection)
        {
            _chatStorage = chatStorage;
            _spamDetection = spamDetection;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext != null)
            {
                var userIdInt = httpContext.Session.GetInt32("UserId");
                var userId = userIdInt?.ToString();
                var userType = httpContext.Session.GetString("UserType");
                var userName = httpContext.Session.GetString("UserName");

                if (!string.IsNullOrEmpty(userId) && (userType == "Staff" || userType == "Admin"))
                {
                    var userKey = $"{userType}_{userId}";
                    ConnectedUsers[userKey] = Context.ConnectionId;
                    OnlineUsers[userKey] = new UserInfo
                    {
                        UserId = userId,
                        UserType = userType,
                        UserName = userName ?? "Unknown"
                    };

                    // Notify all clients about online users update
                    await Clients.All.SendAsync("UpdateOnlineUsers", GetOnlineUsersList());
                }
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext != null)
            {
                var userIdInt = httpContext.Session.GetInt32("UserId");
                var userId = userIdInt?.ToString();
                var userType = httpContext.Session.GetString("UserType");

                if (!string.IsNullOrEmpty(userId))
                {
                    var userKey = $"{userType}_{userId}";
                    ConnectedUsers.TryRemove(userKey, out _);
                    OnlineUsers.TryRemove(userKey, out _);

                    // Notify all clients about online users update
                    await Clients.All.SendAsync("UpdateOnlineUsers", GetOnlineUsersList());
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string receiverType, string receiverId, string message)
        {
            await SendMediaMessage(receiverType, receiverId, message, "Text");
        }

        public async Task SendMediaMessage(string receiverType, string receiverId, string content, string messageType)
        {
            try 
            {
                // Check for sensitive content only for text messages
                if (messageType == "Text" && _spamDetection.CheckSensitiveContent(content, out var detectedWord))
                {
                    throw new HubException($"Message contains sensitive content: '{detectedWord}'");
                }

                var httpContext = Context.GetHttpContext();
                if (httpContext == null) return;

                var userIdInt = httpContext.Session.GetInt32("UserId");
                var senderId = userIdInt?.ToString();
                var senderType = httpContext.Session.GetString("UserType");
                var senderName = httpContext.Session.GetString("UserName");

                if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(senderType)) return;

                var receiverKey = $"{receiverType}_{receiverId}";
                var senderKey = $"{senderType}_{senderId}";

                var chatMessage = new ChatMessage
                {
                    SenderId = senderId,
                    SenderType = senderType,
                    SenderName = senderName ?? "Unknown",
                    ReceiverId = receiverId,
                    ReceiverType = receiverType,
                    Content = content,
                    MessageType = messageType,
                    SentAt = DateTime.Now
                };

                // Save message to storage
                await _chatStorage.SaveMessageAsync(chatMessage);

                // Send to receiver if online
                if (ConnectedUsers.TryGetValue(receiverKey, out var receiverConnectionId))
                {
                    await Clients.Client(receiverConnectionId).SendAsync("ReceiveMessage", chatMessage);
                }

                // Send back to sender (for confirmation)
                await Clients.Caller.SendAsync("MessageSent", chatMessage);
            }
            catch (HubException)
            {
                throw; // Rethrow HubExceptions to be caught by client
            }
            catch (Exception ex)
            {
                // Log generic error
                Console.WriteLine($"Error sending message: {ex.Message}");
                throw new HubException("An error occurred while sending the message.");
            }
        }

        public async Task<List<ChatMessage>> GetChatHistory(string receiverType, string receiverId)
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null) return new List<ChatMessage>();

            var userIdInt = httpContext.Session.GetInt32("UserId");
            var senderId = userIdInt?.ToString();
            var senderType = httpContext.Session.GetString("UserType");

            if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(senderType)) return new List<ChatMessage>();

            return await _chatStorage.GetChatHistoryAsync(senderType, senderId, receiverType, receiverId);
        }

        public async Task WithdrawMessages(List<string> messageIds)
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null) return;

            var userIdInt = httpContext.Session.GetInt32("UserId");
            var senderId = userIdInt?.ToString();
            var senderType = httpContext.Session.GetString("UserType");

            if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(senderType)) return;

            var messages = await _chatStorage.GetMessagesByIdsAsync(messageIds);
            var updates = new List<ChatMessage>();
            
            // Logic: Filter messages that can be withdrawn
            // 1. Must be sender
            // 2. Must be within 2 minutes
            
            foreach (var msg in messages)
            {
                // Verify ownership: Only sender can withdraw
                if (msg.SenderId == senderId && msg.SenderType == senderType)
                {
                    // Verify time limit: Within 2 minutes
                    if ((DateTime.Now - msg.SentAt).TotalMinutes <= 2)
                    {
                        msg.IsWithdrawn = true;
                        updates.Add(msg);
                    }
                }
            }

            if (updates.Any())
            {
                await _chatStorage.UpdateMessagesAsync(updates);
                
                // Notify relevant clients
                foreach (var msg in updates)
                {
                    var receiverKey = $"{msg.ReceiverType}_{msg.ReceiverId}";
                    
                    if (ConnectedUsers.TryGetValue(receiverKey, out var receiverConnectionId))
                    {
                        await Clients.Client(receiverConnectionId).SendAsync("MessagesWithdrawn", new List<string> { msg.Id });
                    }
                     // Also notify sender (current user)
                     await Clients.Caller.SendAsync("MessagesWithdrawn", new List<string> { msg.Id });
                }
            }
        }

        public async Task DeleteMessages(List<string> messageIds)
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null) return;

            var userIdInt = httpContext.Session.GetInt32("UserId");
            var userId = userIdInt?.ToString();
            var userType = httpContext.Session.GetString("UserType");

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userType)) return;

            var messages = await _chatStorage.GetMessagesByIdsAsync(messageIds);
            var updates = new List<ChatMessage>();
            var deletedIds = new List<string>();
            var userKey = $"{userType}_{userId}";

            foreach (var msg in messages)
            {
                // Anyone involved can delete for themselves
                bool isSender = msg.SenderId == userId && msg.SenderType == userType;
                bool isReceiver = msg.ReceiverId == userId && msg.ReceiverType == userType;

                if (isSender || isReceiver)
                {
                    if (!msg.DeletedFor.Contains(userKey))
                    {
                        msg.DeletedFor.Add(userKey);
                        updates.Add(msg);
                        deletedIds.Add(msg.Id);
                    }
                }
            }

            if (updates.Any())
            {
                await _chatStorage.UpdateMessagesAsync(updates);
                await Clients.Caller.SendAsync("MessagesDeleted", deletedIds);
            }
        }

        public List<UserInfo> GetOnlineUsers()
        {
            return GetOnlineUsersList();
        }

        private List<UserInfo> GetOnlineUsersList()
        {
            return OnlineUsers.Values.ToList();
        }
    }

    public class UserInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }

    public class ChatMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SenderId { get; set; } = string.Empty;
        public string SenderType { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public string ReceiverType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string MessageType { get; set; } = "Text"; // Text, Image, Gif
        public DateTime SentAt { get; set; }
        public bool IsWithdrawn { get; set; } = false;
        public List<string> DeletedFor { get; set; } = new List<string>();
    }
}

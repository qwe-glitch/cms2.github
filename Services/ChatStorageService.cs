using System.Text.Json;
using ComplaintManagementSystem.Hubs;

namespace ComplaintManagementSystem.Services
{
    public class ChatStorageService
    {
        private readonly string _filePath;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public ChatStorageService(IWebHostEnvironment env)
        {
            var dataPath = Path.Combine(env.ContentRootPath, "Data");
            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }
            _filePath = Path.Combine(dataPath, "chat_history.json");
        }

        public async Task SaveMessageAsync(ChatMessage message)
        {
            await _lock.WaitAsync();
            try
            {
                var messages = await ReadMessagesInternalAsync();
                messages.Add(message);
                await WriteMessagesInternalAsync(messages);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<List<ChatMessage>> GetChatHistoryAsync(string user1Type, string user1Id, string user2Type, string user2Id)
        {
            await _lock.WaitAsync();
            try
            {
                var messages = await ReadMessagesInternalAsync();
                var viewerKey = $"{user1Type}_{user1Id}";

                return messages.Where(m => 
                    ((m.SenderId == user1Id && m.SenderType == user1Type && m.ReceiverId == user2Id && m.ReceiverType == user2Type) ||
                    (m.SenderId == user2Id && m.SenderType == user2Type && m.ReceiverId == user1Id && m.ReceiverType == user1Type)) &&
                    !m.DeletedFor.Contains(viewerKey)
                )
                .OrderBy(m => m.SentAt)
                .ToList();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<List<ChatMessage>> GetMessagesByIdsAsync(List<string> ids)
        {
            await _lock.WaitAsync();
            try
            {
                var messages = await ReadMessagesInternalAsync();
                return messages.Where(m => ids.Contains(m.Id)).ToList();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task UpdateMessagesAsync(List<ChatMessage> updates)
        {
            await _lock.WaitAsync();
            try
            {
                var messages = await ReadMessagesInternalAsync();
                
                foreach (var update in updates)
                {
                    var index = messages.FindIndex(m => m.Id == update.Id);
                    if (index != -1)
                    {
                        messages[index] = update;
                    }
                }

                await WriteMessagesInternalAsync(messages);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<List<ChatMessage>> ReadMessagesInternalAsync()
        {
            if (!File.Exists(_filePath))
            {
                return new List<ChatMessage>();
            }

            try
            {
                var json = await File.ReadAllTextAsync(_filePath);
                return JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? new List<ChatMessage>();
            }
            catch
            {
                return new List<ChatMessage>();
            }
        }

        private async Task WriteMessagesInternalAsync(List<ChatMessage> messages)
        {
            var json = JsonSerializer.Serialize(messages, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_filePath, json);
        }
    }
}

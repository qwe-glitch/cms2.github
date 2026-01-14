using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ComplaintManagementSystem.Services
{
    public class AiChatService
    {
        private readonly SafeDataService _safeData;
        private readonly ChatService _basicChat;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AiChatService(SafeDataService safeData, ChatService basicChat, HttpClient httpClient, IConfiguration configuration)
        {
            _safeData = safeData;
            _basicChat = basicChat;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> GetSmartResponseAsync(string userMessage)
        {
            try
            {
                // check api key
                var apiKey = _configuration["AISettings:ApiKey"];
                if (string.IsNullOrEmpty(apiKey) || apiKey.StartsWith("YOUR_"))
                {
                    return "AI Setup Required: Please configure the Gemini API Key in appsettings.json. [System Message]";
                }

                // 1. Build the Context (Safe System Data)
                var systemPrompt = await BuildSystemContextAsync(userMessage);
                
                // Combine system prompt and user message because Gemini Free tier via REST often works best 
                // when context is provided as part of the first turn or combined.
                var fullPrompt = $"{systemPrompt}\n\nUSER QUESTION: {userMessage}";

                // 2. Call the AI API
                var response = await CallGeminiApiAsync(fullPrompt);
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AI Error: {ex.Message}");
                return _basicChat.GetResponse(userMessage);
            }
        }

        private async Task<string> CallGeminiApiAsync(string prompt)
        {
            var endpoint = _configuration["AISettings:Endpoint"];
            var apiKey = _configuration["AISettings:ApiKey"];
            
            // Gemini requires key in query param for some endpoints, or header.
            // We'll use the URL provided in settings which should look like:
            // https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent
            
            var url = $"{endpoint}?key={apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"API Error ({response.StatusCode}): {error}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var chatResponse = JsonSerializer.Deserialize<GeminiResponse>(jsonResponse);

            return chatResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "I couldn't generate a response.";
        }

        private async Task<string> BuildSystemContextAsync(string userMessage)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are an intelligent AI assistant for the 'Segamat Complaint Management System'.");
            sb.AppendLine("Your goal is to help citizens and staff by answering questions about the system, status, and processes.");
            sb.AppendLine("------------------------------------------------");
            sb.AppendLine("SAFE SYSTEM CONTEXT (Actual Database Data):");
            
            // Add Schema
            sb.AppendLine(_safeData.GetSafeSchemaDescription());

            // Add Real-time Stats
            sb.AppendLine(await _safeData.GetSafeSystemStatsAsync());

            // Intelligent Context Injection (Search)
            var searchTerm = ExtractSearchTerm(userMessage);
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var searchResults = await _safeData.SearchComplaintsAsync(searchTerm);
                if (!string.IsNullOrWhiteSpace(searchResults))
                {
                    sb.AppendLine(searchResults);
                }
            }

            sb.AppendLine("------------------------------------------------");
            sb.AppendLine("RULES:");
            sb.AppendLine("1. You act as a representative of the system. Be professional and helpful.");
            sb.AppendLine("2. Use the provided SYSTEM CONTEXT to answer questions accurately.");
            sb.AppendLine("3. If the user asks for sensitive info (passwords, admin tokens), politely REFUSE.");
            sb.AppendLine("4. If you don't know the answer, refer them to the Help page or suggest they contact support.");
            
            return sb.ToString(); 
        }

        private string ExtractSearchTerm(string message)
        {
            message = message.ToLower();
            // Simple heuristics to find what the user is looking for
            // e.g. "complain about road", "find about pothole"
            
            string[] triggers = { "about ", "find ", "search for ", "looking for " };
            
            foreach (var trigger in triggers)
            {
                int index = message.IndexOf(trigger);
                if (index != -1)
                {
                    // return everything after the trigger
                    var term = message.Substring(index + trigger.Length).Trim();
                    // cleanup punctuation
                    term = new string(term.Where(c => !char.IsPunctuation(c) || c == ' ').ToArray());
                    return term;
                }
            }
            return string.Empty;
        }

        public async Task<(int Score, string Reasoning)> AnalyzeDuplicationAsync(string title1, string desc1, string title2, string desc2)
        {
            try
            {
                var apiKey = _configuration["AISettings:ApiKey"];
                if (string.IsNullOrEmpty(apiKey) || apiKey.StartsWith("YOUR_"))
                {
                    return (0, "AI not configured.");
                }

                var prompt = $@"
You are a duplication detection expert. Compare these two complaints and determine if they are duplicates.
COMPLAINT 1:
Title: {title1}
Description: {desc1}

COMPLAINT 2:
Title: {title2}
Description: {desc2}

Analyze distinctiveness, location context (if any), and core issue.
Return JSON ONLY: {{ ""score"": 0-100, ""reasoning"": ""short 1 sentence exp"" }}
";
                
                var responseJson = await CallGeminiApiAsync(prompt);
                
                // Parse the response - we expect JSON from the AI
                // We need to clean potential markdown blocks if the AI adds them (e.g. ```json ... ```)
                var cleanJson = responseJson.Replace("```json", "").Replace("```", "").Trim();
                
                using var doc = JsonDocument.Parse(cleanJson);
                var root = doc.RootElement;
                
                int score = 0;
                string reasoning = "No reasoning provided";

                if (root.TryGetProperty("score", out var scoreElement))
                {
                    score = scoreElement.GetInt32();
                }
                
                if (root.TryGetProperty("reasoning", out var reasonElement))
                {
                    reasoning = reasonElement.GetString() ?? reasoning;
                }

                return (score, reasoning);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AI Duplication Check Error: {ex.Message}");
                // Return 0 score on error so it doesn't flag as duplicate
                return (0, "AI Analysis Failed"); 
            }
        }

        // Gemini Response Classes
        public class GeminiResponse
        {
            [JsonPropertyName("candidates")]
            public List<Candidate>? Candidates { get; set; }
        }

        public class Candidate
        {
            [JsonPropertyName("content")]
            public Content? Content { get; set; }
        }

        public class Content
        {
            [JsonPropertyName("parts")]
            public List<Part>? Parts { get; set; }
        }

        public class Part
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }
    }
}

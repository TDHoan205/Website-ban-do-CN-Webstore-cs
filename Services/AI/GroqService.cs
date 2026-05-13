using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Webstore.Services.AI
{
    /// <summary>
    /// Groq AI Service - Sử dụng Groq Cloud API (OpenAI Compatible)
    /// </summary>
    public class GroqService : IGeminiService
    {
        private readonly string _apiKey;
        private readonly string _model;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GroqService> _logger;
        private readonly IGeminiService _geminiFallback; // Dùng để xử lý Embedding

        public GroqService(IConfiguration configuration, ILogger<GroqService> logger, GeminiService geminiFallback)
        {
            _apiKey = configuration["Groq:ApiKey"] ?? "";
            _model = configuration["Groq:Model"] ?? "llama-3.3-70b-versatile";
            _logger = logger;
            _geminiFallback = geminiFallback;
            _httpClient = new HttpClient();
        }

        public async Task<string> GetChatResponseAsync(string systemPrompt, string userMessage, IEnumerable<ChatHistory>? history = null)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("Groq API Key is missing, falling back to Gemini.");
                return await _geminiFallback.GetChatResponseAsync(systemPrompt, userMessage, history);
            }

            try
            {
                var messages = new List<object>();

                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    messages.Add(new { role = "system", content = systemPrompt });
                }

                if (history != null)
                {
                    foreach (var h in history)
                    {
                        messages.Add(new { role = h.Role.ToLower() == "model" ? "assistant" : "user", content = h.Content });
                    }
                }

                messages.Add(new { role = "user", content = userMessage });

                var requestBody = new
                {
                    model = _model,
                    messages = messages,
                    temperature = 0.7,
                    max_tokens = 2048
                };

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
                request.Headers.Add("Authorization", $"Bearer {_apiKey}");
                request.Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Groq API Error: {response.StatusCode} - {responseJson}");
                    // Nếu Groq lỗi (hết quota), fallback về Gemini
                    return await _geminiFallback.GetChatResponseAsync(systemPrompt, userMessage, history);
                }

                using var doc = JsonDocument.Parse(responseJson);
                return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Groq API, falling back to Gemini");
                return await _geminiFallback.GetChatResponseAsync(systemPrompt, userMessage, history);
            }
        }

        public Task<float[]> GetEmbeddingAsync(string text)
        {
            // Groq hiện tại không tập trung vào Embedding, 
            // nên chúng ta vẫn dùng Gemini để lấy vector tìm kiếm sản phẩm.
            return _geminiFallback.GetEmbeddingAsync(text);
        }
    }
}

using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Webstore.Services.AI
{
    /// <summary>
    /// Gemini AI Service - Sử dụng Google Gemini API cho chat và embedding
    /// </summary>
    public interface IGeminiService
    {
        Task<string> GetChatResponseAsync(string systemPrompt, string userMessage, IEnumerable<ChatHistory>? history = null);
        Task<float[]> GetEmbeddingAsync(string text);
    }

    public class ChatHistory
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = "";
    }

    /// <summary>
    /// Gemini API Service - Giao tiếp trực tiếp với Google Gemini API qua REST
    /// </summary>
    public class GeminiService : IGeminiService
    {
        private readonly string _apiKey;
        private readonly string _chatModel;
        private readonly string _embeddingModel;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiService> _logger;

        public const int EmbeddingVectorSize = 768;

        public GeminiService(IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _apiKey = configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini:ApiKey is missing");
            _chatModel = configuration["Gemini:ChatModel"] ?? "gemini-1.5-flash";
            _embeddingModel = configuration["Gemini:EmbeddingModel"] ?? "text-embedding-004";
            _logger = logger;

            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Gửi tin nhắn đến Gemini API và nhận phản hồi
        /// </summary>
        public async Task<string> GetChatResponseAsync(string systemPrompt, string userMessage, IEnumerable<ChatHistory>? history = null)
        {
            try
            {
                var messages = new List<GeminiMessage>();

                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    messages.Add(new GeminiMessage
                    {
                        Role = "user",
                        Parts = new List<GeminiPart>
                        {
                            new GeminiPart { Text = $"System instruction: {systemPrompt}" }
                        }
                    });
                    messages.Add(new GeminiMessage
                    {
                        Role = "model",
                        Parts = new List<GeminiPart>
                        {
                            new GeminiPart { Text = "Tôi đã hiểu. Tôi sẽ tuân thủ các hướng dẫn trên." }
                        }
                    });
                }

                if (history != null)
                {
                    foreach (var h in history)
                    {
                        var role = h.Role.ToLower() == "assistant" || h.Role.ToLower() == "ai" ? "model" : "user";
                        messages.Add(new GeminiMessage
                        {
                            Role = role,
                            Parts = new List<GeminiPart>
                            {
                                new GeminiPart { Text = h.Content }
                            }
                        });
                    }
                }

                messages.Add(new GeminiMessage
                {
                    Role = "user",
                    Parts = new List<GeminiPart>
                    {
                        new GeminiPart { Text = userMessage }
                    }
                });

                var requestBody = new GeminiChatRequest
                {
                    Contents = messages,
                    GenerationConfig = new GeminiGenerationConfig
                    {
                        Temperature = 0.7f,
                        MaxOutputTokens = 2048,
                        TopP = 0.95f,
                        TopK = 40
                    }
                };

                var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{_chatModel}:generateContent?key={_apiKey}";

                var response = await _httpClient.PostAsync(endpoint, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Gemini API Error: {response.StatusCode} - {responseJson}");
                    throw new Exception($"Gemini API Error: {response.StatusCode}");
                }

                var geminiResponse = JsonSerializer.Deserialize<GeminiGenerateResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (geminiResponse?.Candidates != null && geminiResponse.Candidates.Count > 0)
                {
                    var candidate = geminiResponse.Candidates[0];
                    if (candidate.Content?.Parts != null)
                    {
                        return string.Join("", candidate.Content.Parts.Select(p => p.Text ?? ""));
                    }
                }

                return "Xin lỗi, tôi không thể tạo phản hồi lúc này.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                throw;
            }
        }

        /// <summary>
        /// Tạo embedding vector cho văn bản sử dụng Gemini Embedding API
        /// </summary>
        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            try
            {
                var requestBody = new GeminiEmbeddingRequest
                {
                    Model = $"models/{_embeddingModel}",
                    Content = new GeminiEmbeddingContent
                    {
                        Parts = new List<GeminiPart>
                        {
                            new GeminiPart { Text = text }
                        }
                    },
                    TaskType = "RETRIEVAL_DOCUMENT"
                };

                var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{_embeddingModel}:embedContent?key={_apiKey}";

                var response = await _httpClient.PostAsync(endpoint, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Gemini Embedding API Error: {response.StatusCode} - {responseJson}");
                    return new float[EmbeddingVectorSize];
                }

                var embeddingResponse = JsonSerializer.Deserialize<GeminiEmbeddingResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (embeddingResponse?.Embedding?.Values != null && embeddingResponse.Embedding.Values.Count > 0)
                {
                    return embeddingResponse.Embedding.Values.ToArray();
                }

                return new float[EmbeddingVectorSize];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting embedding from Gemini API");
                return new float[EmbeddingVectorSize];
            }
        }
    }

    // Request/Response Models for Gemini API

    public class GeminiChatRequest
    {
        public List<GeminiMessage> Contents { get; set; } = new();
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    public class GeminiMessage
    {
        public string Role { get; set; } = "user";
        public List<GeminiPart> Parts { get; set; } = new();
    }

    public class GeminiPart
    {
        public string? Text { get; set; }
    }

    public class GeminiGenerationConfig
    {
        public float Temperature { get; set; } = 0.7f;
        public int MaxOutputTokens { get; set; } = 2048;
        public float TopP { get; set; } = 0.95f;
        public int TopK { get; set; } = 40;
    }

    public class GeminiGenerateResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
        public string? PromptFeedback { get; set; }
    }

    public class GeminiCandidate
    {
        public GeminiMessage? Content { get; set; }
        public string? FinishReason { get; set; }
        public int? Index { get; set; }
        public GeminiSafetyRatings? SafetyRatings { get; set; }
    }

    public class GeminiSafetyRatings
    {
        public string? Category { get; set; }
        public string? Probability { get; set; }
    }

    // Embedding Models
    public class GeminiEmbeddingRequest
    {
        public string Model { get; set; } = "";
        public GeminiEmbeddingContent Content { get; set; } = new();
        public string TaskType { get; set; } = "RETRIEVAL_DOCUMENT";
    }

    public class GeminiEmbeddingContent
    {
        public List<GeminiPart> Parts { get; set; } = new();
    }

    public class GeminiEmbeddingResponse
    {
        public GeminiEmbedding? Embedding { get; set; }
    }

    public class GeminiEmbedding
    {
        public List<float> Values { get; set; } = new();
    }
}

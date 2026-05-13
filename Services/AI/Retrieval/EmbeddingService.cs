using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Webstore.Services.AI
{
    /// <summary>
    /// Semantic text embedding using Gemini.
    /// </summary>
    public class EmbeddingService
    {
        private readonly GeminiService _gemini;
        private readonly IMemoryCache _cache;
        public const int VectorSize = 768; // Gemini embedding-001 dimension

        public EmbeddingService(GeminiService gemini, IMemoryCache cache)
        {
            _gemini = gemini;
            _cache = cache;
        }

        public string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return input.Trim().ToLowerInvariant();
        }

        public async Task<float[]> BuildEmbeddingAsync(string input)
        {
            var normalized = Normalize(input);
            if (string.IsNullOrEmpty(normalized))
                return new float[VectorSize];

            // Cache key for embedding
            string cacheKey = $"emb_{normalized}";

            if (_cache.TryGetValue(cacheKey, out float[]? cachedVector) && cachedVector != null)
            {
                return cachedVector;
            }

            var vector = await _gemini.GetEmbeddingAsync(normalized);

            // Cache the vector for 1 hour
            if (vector.Any(v => v != 0))
            {
                _cache.Set(cacheKey, vector, TimeSpan.FromHours(1));
            }

            return vector;
        }

        public string Serialize(float[] vector)
        {
            return JsonSerializer.Serialize(vector);
        }

        public float[] Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new float[VectorSize];

            try
            {
                return JsonSerializer.Deserialize<float[]>(json) ?? new float[VectorSize];
            }
            catch
            {
                return new float[VectorSize];
            }
        }

        public double CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length == 0 || b.Length == 0)
                return 0d;

            var length = Math.Min(a.Length, b.Length);
            double dot = 0;
            double normA = 0;
            double normB = 0;

            for (var i = 0; i < length; i++)
            {
                dot += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            if (normA <= 0 || normB <= 0)
                return 0d;

            return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        private static int HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return BitConverter.ToInt32(bytes, 0);
        }

        private static void NormalizeL2(float[] vector)
        {
            double norm = 0;
            for (var i = 0; i < vector.Length; i++)
            {
                norm += vector[i] * vector[i];
            }

            if (norm <= 0)
                return;

            var inv = (float)(1.0 / Math.Sqrt(norm));
            for (var i = 0; i < vector.Length; i++)
            {
                vector[i] *= inv;
            }
        }
    }
}

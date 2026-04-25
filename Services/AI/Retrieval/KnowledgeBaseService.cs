using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Webstore.Data;
using Webstore.Models.AI;

namespace Webstore.Services.AI
{
    public class KnowledgeSearchResult
    {
        public KnowledgeChunk Chunk { get; set; } = new();
        public double Similarity { get; set; }
    }

    /// <summary>
    /// Build and query local vector knowledge base for RAG.
    /// </summary>
    public class KnowledgeBaseService
    {
        private readonly ApplicationDbContext _context;
        private readonly EmbeddingService _embedding;

        public KnowledgeBaseService(ApplicationDbContext context, EmbeddingService embedding)
        {
            _context = context;
            _embedding = embedding;
        }

        public async Task BuildOrRefreshAsync(bool forceRebuild = false)
        {
            var hasChunks = await _context.KnowledgeChunks.AnyAsync();
            if (hasChunks && !forceRebuild)
                return;

            if (forceRebuild)
            {
                _context.KnowledgeChunks.RemoveRange(_context.KnowledgeChunks);
                await _context.SaveChangesAsync();
            }

            if (!hasChunks || forceRebuild)
            {
                await BuildChunksAsync();
            }
        }

        public async Task<List<KnowledgeSearchResult>> SearchAsync(
            string query,
            int topK = 5,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? category = null)
        {
            var normalizedQuery = _embedding.Normalize(query);
            var queryVector = await _embedding.BuildEmbeddingAsync(normalizedQuery);

            var dbQuery = _context.KnowledgeChunks.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                dbQuery = dbQuery.Where(c => c.SourceType != "product" || c.Category == category);
            }

            if (minPrice.HasValue)
            {
                dbQuery = dbQuery.Where(c => c.SourceType != "product" || (c.Price.HasValue && c.Price.Value >= minPrice.Value));
            }

            if (maxPrice.HasValue)
            {
                dbQuery = dbQuery.Where(c => c.SourceType != "product" || (c.Price.HasValue && c.Price.Value <= maxPrice.Value));
            }

            var candidates = await dbQuery.ToListAsync();
            var results = candidates
                .Select(c =>
                {
                    var chunkVector = _embedding.Deserialize(c.Embedding);
                    var semanticScore = _embedding.CosineSimilarity(queryVector, chunkVector);
                    var lexicalBoost = !string.IsNullOrWhiteSpace(normalizedQuery) && c.NormalizedText.Contains(normalizedQuery) ? 0.15 : 0;
                    var priorityBoost = c.Priority * 0.01;

                    return new KnowledgeSearchResult
                    {
                        Chunk = c,
                        Similarity = semanticScore + lexicalBoost + priorityBoost
                    };
                })
                .OrderByDescending(r => r.Similarity)
                .Take(Math.Clamp(topK, 1, 10))
                .ToList();

            return results;
        }

        private async Task BuildChunksAsync()
        {
            var chunks = new List<KnowledgeChunk>();

            var products = await _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .Where(p => p.Price > 0)
                .ToListAsync();

            foreach (var p in products)
            {
                var baseText = $"{p.Name}. {p.Description}";
                foreach (var sentenceChunk in SplitIntoChunks(baseText, 500)) // Increased chunk size for better context with LLMs
                {
                    chunks.Add(await CreateChunkAsync(
                        sourceType: "product",
                        sourceId: p.ProductId,
                        chunkType: "product_desc",
                        rawText: sentenceChunk,
                        price: p.Price,
                        category: p.Category?.Name,
                        priority: 5));
                }
            }

            var faqs = await _context.FAQs
                .AsNoTracking()
                .Where(f => f.IsActive)
                .ToListAsync();

            foreach (var faq in faqs)
            {
                chunks.Add(await CreateChunkAsync(
                    sourceType: "faq",
                    sourceId: faq.FaqId,
                    chunkType: "faq_question",
                    rawText: faq.Question,
                    category: faq.Category,
                    priority: faq.Priority));

                foreach (var answerChunk in SplitIntoChunks(faq.Answer, 500))
                {
                    chunks.Add(await CreateChunkAsync(
                        sourceType: "faq",
                        sourceId: faq.FaqId,
                        chunkType: "faq_answer",
                        rawText: answerChunk,
                        category: faq.Category,
                        priority: faq.Priority));
                }
            }

            await _context.KnowledgeChunks.AddRangeAsync(chunks);
            await _context.SaveChangesAsync();
        }

        private async Task<KnowledgeChunk> CreateChunkAsync(
            string sourceType,
            int sourceId,
            string chunkType,
            string rawText,
            decimal? price = null,
            string? category = null,
            int priority = 0)
        {
            var normalized = _embedding.Normalize(rawText);
            var vector = await _embedding.BuildEmbeddingAsync(normalized);

            return new KnowledgeChunk
            {
                SourceType = sourceType,
                SourceId = sourceId,
                ChunkType = chunkType,
                RawText = rawText.Trim(),
                NormalizedText = normalized,
                Embedding = _embedding.Serialize(vector),
                Price = price,
                Category = category,
                Priority = priority,
                CreatedAt = DateTime.Now
            };
        }

        private static IEnumerable<string> SplitIntoChunks(string? text, int maxChunkLength)
        {
            if (string.IsNullOrWhiteSpace(text))
                yield break;

            var cleaned = Regex.Replace(text, "\\s+", " ").Trim();
            if (cleaned.Length <= maxChunkLength)
            {
                yield return cleaned;
                yield break;
            }

            var sentences = Regex.Split(cleaned, @"(?<=[\.!?])\s+");
            var current = string.Empty;

            foreach (var sentence in sentences)
            {
                if (string.IsNullOrWhiteSpace(sentence))
                    continue;

                var candidate = string.IsNullOrEmpty(current)
                    ? sentence
                    : $"{current} {sentence}";

                if (candidate.Length <= maxChunkLength)
                {
                    current = candidate;
                    continue;
                }

                if (!string.IsNullOrEmpty(current))
                {
                    yield return current;
                    current = string.Empty;
                }

                if (sentence.Length <= maxChunkLength)
                {
                    current = sentence;
                    continue;
                }

                var index = 0;
                while (index < sentence.Length)
                {
                    var size = Math.Min(maxChunkLength, sentence.Length - index);
                    yield return sentence.Substring(index, size).Trim();
                    index += size;
                }
            }

            if (!string.IsNullOrEmpty(current))
            {
                yield return current;
            }
        }
    }
}

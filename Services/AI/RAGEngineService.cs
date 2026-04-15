using Microsoft.EntityFrameworkCore;
using Webstore.Data;
using Webstore.Models.AI;

namespace Webstore.Services.AI
{
    /// <summary>
    /// RAG Engine - Retrieval Augmented Generation cho AI Chat
    /// </summary>
    public class RAGEngineService
    {
        private readonly ApplicationDbContext _context;
        private readonly IntentDetectionService _intentDetector;

        public RAGEngineService(ApplicationDbContext context, IntentDetectionService intentDetector)
        {
            _context = context;
            _intentDetector = intentDetector;
        }

        /// <summary>
        /// Lấy context cho AI từ database
        /// </summary>
        public async Task<RAGContext> GetContextAsync(string userMessage)
        {
            var intent = _intentDetector.DetectIntent(userMessage);
            var confidence = _intentDetector.GetConfidenceScore(userMessage, intent);

            var faqs = await GetRelevantFAQsAsync(userMessage, intent);
            var products = await GetRelevantProductsAsync(userMessage);
            var shouldEscalate = _intentDetector.ShouldEscalate(intent, confidence);

            return new RAGContext
            {
                Intent = intent,
                Confidence = confidence,
                FAQs = faqs,
                Products = products,
                ShouldEscalate = shouldEscalate
            };
        }

        /// <summary>
        /// Lấy FAQ liên quan dựa trên intent
        /// </summary>
        private async Task<List<FAQ>> GetRelevantFAQsAsync(string userMessage, string intent)
        {
            var lowerMessage = userMessage.ToLower();

            var faqs = await _context.FAQs
                .Where(f => f.IsActive)
                .ToListAsync();

            // Filter by category if intent matches
            var categoryMap = new Dictionary<string, string>
            {
                ["purchase"] = "purchase",
                ["payment"] = "payment",
                ["warranty"] = "warranty",
                ["shipping"] = "shipping",
                ["order_status"] = "general"
            };

            if (categoryMap.ContainsKey(intent))
            {
                var category = categoryMap[intent];
                faqs = faqs.Where(f => f.Category == category || f.Category == "general").ToList();
            }

            // Filter by keywords in message
            if (!string.IsNullOrEmpty(lowerMessage))
            {
                faqs = faqs.Where(f =>
                {
                    var question = f.Question?.ToLower() ?? "";
                    var keywords = f.Keywords?.ToLower() ?? "";
                    return question.Contains(lowerMessage.Split(' ').FirstOrDefault() ?? "") ||
                           keywords.Split(',').Any(k => lowerMessage.Contains(k.Trim()));
                }).ToList();
            }

            // Sort by priority and take top 5
            return faqs
                .OrderByDescending(f => f.Priority)
                .Take(5)
                .ToList();
        }

        /// <summary>
        /// Lấy sản phẩm liên quan dựa trên message
        /// </summary>
        private async Task<List<ProductContext>> GetRelevantProductsAsync(string userMessage)
        {
            var lowerMessage = userMessage.ToLower();
            var products = new List<ProductContext>();

            // Parse budget from message
            decimal? minBudget = null;
            decimal? maxBudget = null;

            // Extract price ranges
            var pricePatterns = new[]
            {
                @"(\d+)\s*triệu",
                @"(\d+)\s*tr",
                @"từ\s*(\d+)\s*tr",
                @"đến\s*(\d+)\s*tr",
                @"dưới\s*(\d+)\s*tr",
                @"trên\s*(\d+)\s*tr"
            };

            foreach (var pattern in pricePatterns)
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(lowerMessage, pattern);
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (decimal.TryParse(match.Groups[1].Value, out var price))
                    {
                        if (lowerMessage.Contains("dưới"))
                            maxBudget = price * 1_000_000;
                        else if (lowerMessage.Contains("trên"))
                            minBudget = price * 1_000_000;
                        else if (lowerMessage.Contains("từ") && matches.Count > 1)
                            minBudget = price * 1_000_000;
                        else if (lowerMessage.Contains("đến") && matches.Count > 1)
                            maxBudget = price * 1_000_000;
                        else
                            maxBudget = price * 1_000_000;
                    }
                }
            }

            // Determine category from message
            string? categoryFilter = null;
            if (lowerMessage.Contains("laptop") || lowerMessage.Contains("máy tính"))
                categoryFilter = "Laptop";
            else if (lowerMessage.Contains("điện thoại") || lowerMessage.Contains("phone") || lowerMessage.Contains("đt"))
                categoryFilter = "Điện thoại";
            else if (lowerMessage.Contains("tablet") || lowerMessage.Contains("máy tính bảng"))
                categoryFilter = "Tablet";
            else if (lowerMessage.Contains("phụ kiện") || lowerMessage.Contains("tai nghe") || lowerMessage.Contains("sạc"))
                categoryFilter = "Phụ kiện";

            // Query products
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.Price > 0);

            if (!string.IsNullOrEmpty(categoryFilter))
            {
                query = query.Where(p => p.Category != null && p.Category.Name == categoryFilter);
            }

            if (minBudget.HasValue)
            {
                query = query.Where(p => p.Price >= minBudget.Value);
            }

            if (maxBudget.HasValue)
            {
                query = query.Where(p => p.Price <= maxBudget.Value);
            }

            var dbProducts = await query
                .OrderByDescending(p => p.Price)
                .Take(5)
                .ToListAsync();

            return dbProducts.Select(p => new ProductContext
            {
                ProductId = p.ProductId,
                Name = p.Name ?? "",
                Price = p.Price,
                Category = p.Category?.Name ?? "",
                Specs = p.Description ?? "",
                ImageUrl = p.ImageUrl ?? ""
            }).ToList();
        }
    }

    /// <summary>
    /// RAG Context - dữ liệu context cho AI
    /// </summary>
    public class RAGContext
    {
        public string Intent { get; set; } = "general";
        public decimal Confidence { get; set; } = 0.5m;
        public List<FAQ> FAQs { get; set; } = new();
        public List<ProductContext> Products { get; set; } = new();
        public bool ShouldEscalate { get; set; } = false;
    }

    /// <summary>
    /// Product Context - thông tin sản phẩm cho AI
    /// </summary>
    public class ProductContext
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public string Category { get; set; } = "";
        public string Specs { get; set; } = "";
        public string ImageUrl { get; set; } = "";
    }
}

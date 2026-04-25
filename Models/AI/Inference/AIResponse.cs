using Webstore.Models.AI;

namespace Webstore.Models.AI
{
    /// <summary>
    /// AI Response - phản hồi từ AI
    /// </summary>
    public class AIResponse
    {
        public string Message { get; set; } = string.Empty;
        public string Intent { get; set; } = string.Empty;
        public decimal Confidence { get; set; }
        public bool ShouldEscalate { get; set; }
        public List<ProductContext> Products { get; set; } = new();
        public List<FAQ> Faqs { get; set; } = new();
    }
}

namespace Webstore.Models.AI
{
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
}

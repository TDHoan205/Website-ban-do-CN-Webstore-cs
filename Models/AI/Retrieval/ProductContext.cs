namespace Webstore.Models.AI
{
    /// <summary>
    /// Product Context - thông tin sản phẩm cho AI
    /// </summary>
    public class ProductContext
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Specs { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }
}

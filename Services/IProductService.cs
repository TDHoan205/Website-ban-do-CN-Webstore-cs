using Webstore.Models;

namespace Webstore.Services
{
    public interface IProductService
    {
        Task<PagedList<Product>> GetProductsAsync(string? search, int? categoryId, string? sortBy, int page, int pageSize, decimal? minPrice = null, decimal? maxPrice = null);
        Task<Product?> GetProductByIdAsync(int id);
        Task<IEnumerable<Product>> GetRelatedProductsAsync(int productId, int categoryId, int count);
        Task<IEnumerable<Product>> GetFeaturedProductsAsync(string type, int count);
        Task<IEnumerable<Category>> GetAllCategoriesAsync();
        Task<IEnumerable<object>> SearchRealtime(string q, int count);
        Task<ProductFiltersViewModel> GetFiltersAsync(int? categoryId);
        Task CreateProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(int id);
        void InvalidateCache();
    }


    public class ProductFiltersViewModel
    {
        public List<string> Colors { get; set; } = new();
        public List<string> Storages { get; set; } = new();
        public List<string> RAMs { get; set; } = new();
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
    }
}

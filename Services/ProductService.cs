using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Webstore.Data;
using Webstore.Models;

namespace Webstore.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);

        public ProductService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<PagedList<Product>> GetProductsAsync(string? search, int? categoryId, string? sortBy, int page, int pageSize, decimal? minPrice = null, decimal? maxPrice = null, string? filter = null)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Include(p => p.Variants)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var terms = search.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in terms)
                {
                    var t = term.Trim();
                    if (string.IsNullOrEmpty(t)) continue;

                    query = query.Where(p => 
                        p.Name.Contains(t) || 
                        (p.Description != null && p.Description.Contains(t)) ||
                        (p.Category != null && p.Category.Name.Contains(t)) ||
                        (p.Supplier != null && p.Supplier.Name.Contains(t)) ||
                        p.ProductId.ToString() == t ||
                        p.Variants.Any(v => 
                            (v.Color != null && v.Color.Contains(t)) || 
                            (v.Storage != null && v.Storage.Contains(t)) || 
                            (v.RAM != null && v.RAM.Contains(t))
                        )
                    );
                }
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value || p.Variants.Any(v => v.Price >= minPrice.Value));
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value || (p.Variants.Any() && p.Variants.Min(v => v.Price) <= maxPrice.Value));
            }

            if (!string.IsNullOrWhiteSpace(filter))
            {
                switch (filter.ToLower())
                {
                    case "new":
                        query = query.Where(p => p.IsNew);
                        break;
                    case "hot":
                        query = query.Where(p => p.IsHot);
                        break;
                    case "deal":
                        query = query.Where(p => p.DiscountPercent > 0 || (p.OriginalPrice.HasValue && p.OriginalPrice > p.Price));
                        break;
                }
            }

            query = sortBy switch
            {
                "name" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                "price" => query.OrderBy(p => p.Price),
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "category" => query.OrderBy(p => p.Category!.Name),
                "category_desc" => query.OrderByDescending(p => p.Category!.Name),
                "supplier" => query.OrderBy(p => p.Supplier!.Name),
                "supplier_desc" => query.OrderByDescending(p => p.Supplier!.Name),
                "newest" => query.OrderByDescending(p => p.ProductId),
                _ => query.OrderByDescending(p => p.ProductId) // Default to newest
            };

            return await PagedList<Product>.CreateAsync(query, page, pageSize);
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            var cacheKey = $"product_{id}";
            
            if (_cache.TryGetValue(cacheKey, out Product? cachedProduct) && cachedProduct != null)
            {
                return cachedProduct;
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Include(p => p.Inventory)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product != null)
            {
                _cache.Set(cacheKey, product, DefaultCacheDuration);
            }

            return product;
        }

        public async Task<IEnumerable<Product>> GetRelatedProductsAsync(int productId, int categoryId, int count)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == categoryId && p.ProductId != productId)
                .OrderByDescending(p => p.ProductId)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetFeaturedProductsAsync(string type, int count)
        {
            var cacheKey = $"featured_{type}_{count}";
            
            if (_cache.TryGetValue(cacheKey, out IEnumerable<Product>? cachedProducts))
            {
                return cachedProducts ?? Enumerable.Empty<Product>();
            }

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .AsQueryable();

            switch (type.ToLower())
            {
                case "new":
                    query = query.OrderByDescending(p => p.ProductId);
                    break;
                case "hot":
                    query = query.Where(p => p.IsHot).OrderByDescending(p => p.ProductId);
                    break;
                case "deal":
                    query = query
                        .Where(p => p.DiscountPercent > 0 || (p.OriginalPrice.HasValue && p.OriginalPrice > p.Price))
                        .OrderByDescending(p => p.ProductId);
                    break;
                default:
                    query = query.OrderByDescending(p => p.ProductId);
                    break;
            }

            // Remove Distinct and use proper pagination - distinct before orderby can cause issues
            var products = await query.Take(count).ToListAsync();
            _cache.Set(cacheKey, products, DefaultCacheDuration);

            return products;
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            var cacheKey = "all_categories_with_products";
            
            if (_cache.TryGetValue(cacheKey, out IEnumerable<Category>? cachedCategories))
            {
                return cachedCategories ?? Enumerable.Empty<Category>();
            }

            // Only get categories that have at least one product
            var categories = await _context.Categories
                .Where(c => _context.Products.Any(p => p.CategoryId == c.CategoryId))
                .ToListAsync();

            _cache.Set(cacheKey, categories, TimeSpan.FromMinutes(15));

            return categories;
        }

        public async Task<IEnumerable<object>> SearchRealtime(string q, int count)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2) return new List<object>();

            var t = q.Trim();
            var results = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Where(p => 
                    p.Name.Contains(t) || 
                    (p.Description != null && p.Description.Contains(t)) ||
                    (p.Category != null && p.Category.Name.Contains(t)) ||
                    p.ProductId.ToString() == t ||
                    p.Variants.Any(v => 
                        (v.Color != null && v.Color.Contains(t)) || 
                        (v.Storage != null && v.Storage.Contains(t)) || 
                        (v.RAM != null && v.RAM.Contains(t))
                    )
                )
                .Take(count)
                .Select(p => new
                {
                    p.ProductId,
                    p.Name,
                    p.Price,
                    p.ImageUrl,
                    CategoryName = p.Category != null ? p.Category.Name : ""
                })
                .ToListAsync();

            return results.Cast<object>();
        }

        public async Task<ProductFiltersViewModel> GetFiltersAsync(int? categoryId)
        {
            var cacheKey = $"filters_{categoryId}";
            
            if (_cache.TryGetValue(cacheKey, out ProductFiltersViewModel? cachedFilters))
            {
                return cachedFilters ?? new ProductFiltersViewModel();
            }

            var query = _context.ProductVariants.AsQueryable();
            if (categoryId.HasValue)
            {
                query = query.Where(v => v.Product != null && v.Product.CategoryId == categoryId.Value);
            }

            var colors = await query.Where(v => v.Color != null).Select(v => v.Color!).Distinct().ToListAsync();
            var storages = await query.Where(v => v.Storage != null).Select(v => v.Storage!).Distinct().ToListAsync();
            var rams = await query.Where(v => v.RAM != null).Select(v => v.RAM!).Distinct().ToListAsync();

            var minPrice = await _context.Products.AnyAsync() ? await _context.Products.MinAsync(p => p.Price) : 0;
            var maxPrice = await _context.Products.AnyAsync() ? await _context.Products.MaxAsync(p => p.Price) : 0;

            var filters = new ProductFiltersViewModel
            {
                Colors = colors,
                Storages = storages,
                RAMs = rams,
                MinPrice = minPrice,
                MaxPrice = maxPrice
            };

            _cache.Set(cacheKey, filters, DefaultCacheDuration);

            return filters;
        }

        public async Task CreateProductAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            InvalidateProductCache();
        }

        public async Task UpdateProductAsync(Product product)
        {
            _context.Entry(product).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            InvalidateProductCache();
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                InvalidateProductCache();
            }
        }

        public void InvalidateCache()
        {
            InvalidateProductCache();
        }

        private void InvalidateProductCache()
        {
            _cache.Remove("all_categories");
            _cache.Remove("filters_");
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Webstore.Data;
using Webstore.Data.Repositories;
using Webstore.Models;

namespace Webstore.Services
{
    public class CartService : ICartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IProductRepository _productRepo;
        private readonly IRepository<ProductVariant> _variantRepo;
        private const string CartSessionKey = "Cart";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };

        public CartService(IHttpContextAccessor httpContextAccessor, IProductRepository productRepo, IRepository<ProductVariant> variantRepo)
        {
            _httpContextAccessor = httpContextAccessor;
            _productRepo = productRepo;
            _variantRepo = variantRepo;
        }

        private ISession? Session => _httpContextAccessor.HttpContext?.Session;

        public List<CartItem> GetCartItems()
        {
            var cartJson = Session?.GetString(CartSessionKey);
            return string.IsNullOrEmpty(cartJson) 
                ? new List<CartItem>() 
                : JsonSerializer.Deserialize<List<CartItem>>(cartJson, JsonOptions) ?? new List<CartItem>();
        }

        public async Task<List<CartItem>> GetCartItemsAsync()
        {
            var cartItems = GetCartItems();

            if (cartItems.Any())
            {
                var productIds = cartItems.Select(c => c.ProductId).Distinct().ToList();
                var variantIds = cartItems.Where(c => c.VariantId.HasValue).Select(c => c.VariantId!.Value).Distinct().ToList();

                var products = (await _productRepo.FindAsync(p => productIds.Contains(p.ProductId))).ToDictionary(p => p.ProductId);
                var variants = variantIds.Any() 
                    ? (await _variantRepo.FindAsync(v => variantIds.Contains(v.VariantId))).ToDictionary(v => v.VariantId)
                    : new Dictionary<int, ProductVariant>();

                foreach (var item in cartItems)
                {
                    if (products.TryGetValue(item.ProductId, out var product)) item.Product = product;
                    if (item.VariantId.HasValue && variants.TryGetValue(item.VariantId.Value, out var variant)) item.Variant = variant;
                }
            }

            return cartItems;
        }

        public async Task SaveCartItemsAsync(List<CartItem> cartItems)
        {
            var cartJson = JsonSerializer.Serialize(cartItems, JsonOptions);
            Session?.SetString(CartSessionKey, cartJson);
            await Task.CompletedTask;
        }

        public Task SaveCartItems(List<CartItem> cartItems)
        {
            return SaveCartItemsAsync(cartItems);
        }

        public async Task AddToCartAsync(int productId, int? variantId, int quantity)
        {
            var cartItems = GetCartItems();
            var item = cartItems.FirstOrDefault(c => c.ProductId == productId && c.VariantId == variantId);

            if (item != null)
            {
                item.Quantity += quantity;
            }
            else
            {
                cartItems.Add(new CartItem
                {
                    ProductId = productId,
                    VariantId = variantId,
                    Quantity = quantity,
                    AddedDate = DateTime.Now
                });
            }

            await SaveCartItemsAsync(cartItems);
        }

        public async Task UpdateQuantityAsync(int productId, int? variantId, int quantity)
        {
            var cartItems = GetCartItems();
            var item = cartItems.FirstOrDefault(c => c.ProductId == productId && c.VariantId == variantId);

            if (item != null)
            {
                if (quantity > 0) item.Quantity = quantity;
                else cartItems.Remove(item);
                
                await SaveCartItemsAsync(cartItems);
            }
        }

        public async Task RemoveFromCartAsync(int productId, int? variantId)
        {
            var cartItems = GetCartItems();
            var item = cartItems.FirstOrDefault(c => c.ProductId == productId && c.VariantId == variantId);

            if (item != null)
            {
                cartItems.Remove(item);
                await SaveCartItemsAsync(cartItems);
            }
        }

        public async Task ClearCart()
        {
            Session?.Remove(CartSessionKey);
            await Task.CompletedTask;
        }

        public async Task<int> GetCartCount()
        {
            return (await GetCartItemsAsync()).Sum(c => c.Quantity);
        }
    }
}

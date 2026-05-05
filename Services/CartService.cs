using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Webstore.Data;
using Webstore.Models;

namespace Webstore.Services
{
    public class CartService : ICartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _context;
        private const string GuestCartCookie = "guest_cart_id";

        public CartService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        private HttpContext? HttpContext => _httpContextAccessor.HttpContext;

        private int? GetAccountId()
        {
            var claim = HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(claim) && int.TryParse(claim, out var id)) return id;
            return null;
        }

        private string? GetGuestSessionId(bool createIfMissing)
        {
            if (HttpContext == null) return null;

            if (HttpContext.Request.Cookies.TryGetValue(GuestCartCookie, out var existing) && !string.IsNullOrWhiteSpace(existing))
            {
                return existing;
            }

            if (!createIfMissing) return null;

            var newId = Guid.NewGuid().ToString("N");
            HttpContext.Response.Cookies.Append(GuestCartCookie, newId, new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
            return newId;
        }

        private async Task<Cart?> GetCartAsync(int? accountId, string? sessionId)
        {
            if (accountId.HasValue)
            {
                return await _context.Carts.Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.AccountId == accountId.Value);
            }

            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                return await _context.Carts.Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.SessionId == sessionId);
            }

            return null;
        }

        private async Task<Cart> GetOrCreateCartAsync(int? accountId, string? sessionId)
        {
            var cart = await GetCartAsync(accountId, sessionId);
            if (cart != null) return cart;

            var roleName = HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;
            cart = new Cart
            {
                AccountId = accountId,
                SessionId = sessionId,
                RoleName = accountId.HasValue ? roleName : "Guest",
                CreatedAt = DateTime.UtcNow
            };

            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
            return cart;
        }

        public List<CartItem> GetCartItems()
        {
            return GetCartItemsAsync().GetAwaiter().GetResult();
        }

        public async Task<List<CartItem>> GetCartItemsAsync()
        {
            var accountId = GetAccountId();
            var sessionId = accountId.HasValue ? null : GetGuestSessionId(false);
            var cart = await GetCartAsync(accountId, sessionId);
            if (cart == null) return new List<CartItem>();

            return await _context.CartItems
                .Include(ci => ci.Product).ThenInclude(p => p!.Category)
                .Include(ci => ci.Variant)
                .Where(ci => ci.CartId == cart.CartId)
                .OrderByDescending(ci => ci.AddedDate)
                .ToListAsync();
        }

        public Task SaveCartItems(List<CartItem> cartItems)
        {
            return SaveCartItemsAsync(cartItems);
        }

        public async Task SaveCartItemsAsync(List<CartItem> cartItems)
        {
            if (!cartItems.Any()) return;

            foreach (var item in cartItems)
            {
                var existing = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartId == item.CartId && ci.ProductId == item.ProductId && ci.VariantId == item.VariantId);

                if (existing == null)
                {
                    _context.CartItems.Add(item);
                }
                else
                {
                    existing.Quantity = item.Quantity;
                    existing.AddedDate = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task AddToCartAsync(int productId, int? variantId, int quantity)
        {
            if (quantity <= 0) throw new InvalidOperationException("Số lượng không hợp lệ.");

            var accountId = GetAccountId();
            var sessionId = accountId.HasValue ? null : GetGuestSessionId(true);
            var cart = await GetOrCreateCartAsync(accountId, sessionId);

            if (variantId.HasValue)
            {
                var variantOk = await _context.ProductVariants.AnyAsync(v => v.VariantId == variantId && v.ProductId == productId);
                if (!variantOk) throw new InvalidOperationException("Biến thể không hợp lệ.");
            }

            var item = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.CartId && ci.ProductId == productId && ci.VariantId == variantId);

            if (item != null)
            {
                item.Quantity += quantity;
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = productId,
                    VariantId = variantId,
                    Quantity = quantity,
                    AddedDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateQuantityAsync(int productId, int? variantId, int quantity)
        {
            var accountId = GetAccountId();
            var sessionId = accountId.HasValue ? null : GetGuestSessionId(false);
            var cart = await GetCartAsync(accountId, sessionId);
            if (cart == null) return;

            var item = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.CartId && ci.ProductId == productId && ci.VariantId == variantId);

            if (item == null) return;

            if (quantity > 0)
            {
                item.Quantity = quantity;
            }
            else
            {
                _context.CartItems.Remove(item);
            }

            await _context.SaveChangesAsync();
        }

        public async Task RemoveFromCartAsync(int productId, int? variantId)
        {
            var accountId = GetAccountId();
            var sessionId = accountId.HasValue ? null : GetGuestSessionId(false);
            var cart = await GetCartAsync(accountId, sessionId);
            if (cart == null) return;

            var item = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.CartId && ci.ProductId == productId && ci.VariantId == variantId);

            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearCart()
        {
            var accountId = GetAccountId();
            var sessionId = accountId.HasValue ? null : GetGuestSessionId(false);
            var cart = await GetCartAsync(accountId, sessionId);
            if (cart == null) return;

            var items = await _context.CartItems.Where(ci => ci.CartId == cart.CartId).ToListAsync();
            if (items.Any())
            {
                _context.CartItems.RemoveRange(items);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetCartCount()
        {
            return (await GetCartItemsAsync()).Sum(c => c.Quantity);
        }

        public async Task MergeGuestCartAsync(int accountId)
        {
            var guestId = GetGuestSessionId(false);
            if (string.IsNullOrWhiteSpace(guestId)) return;

            var guestCart = await GetCartAsync(null, guestId);
            if (guestCart == null) return;

            var userCart = await GetOrCreateCartAsync(accountId, null);

            var guestItems = await _context.CartItems
                .Where(ci => ci.CartId == guestCart.CartId)
                .ToListAsync();

            foreach (var item in guestItems)
            {
                var existing = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartId == userCart.CartId && ci.ProductId == item.ProductId && ci.VariantId == item.VariantId);

                if (existing == null)
                {
                    item.CartId = userCart.CartId;
                    _context.CartItems.Update(item);
                }
                else
                {
                    existing.Quantity += item.Quantity;
                    _context.CartItems.Remove(item);
                }
            }

            _context.Carts.Remove(guestCart);
            await _context.SaveChangesAsync();

            if (HttpContext != null)
            {
                HttpContext.Response.Cookies.Delete(GuestCartCookie);
            }
        }
    }
}

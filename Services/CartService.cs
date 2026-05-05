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

        /// <summary>Trả về inner-most exception message cho dễ đọc</summary>
        private static string GetInnermostMessage(Exception ex)
        {
            var current = ex;
            while (current.InnerException != null) current = current.InnerException;
            return current.Message;
        }

        /// <summary>Log chi tiết lỗi EF</summary>
        private static string FormatEfError(Exception ex, string context)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[CartService] {context}");
            sb.AppendLine($"  Outer: {ex.Message}");
            if (ex is DbUpdateException dbEx)
            {
                sb.AppendLine($"  DbUpdate: {dbEx.Message}");
                if (dbEx.InnerException != null)
                {
                    sb.AppendLine($"  Inner: {dbEx.InnerException.Message}");
                    if (dbEx.InnerException.InnerException != null)
                        sb.AppendLine($"  Inner2: {dbEx.InnerException.InnerException.Message}");
                }
                sb.AppendLine($"  Entries: {string.Join(", ", dbEx.Entries.Select(e => $"{e.Entity.GetType().Name} (State:{e.State})"))}");
            }
            else if (ex.InnerException != null)
            {
                sb.AppendLine($"  Inner: {ex.InnerException.Message}");
            }
            return sb.ToString();
        }

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
                // Same NULL=NULL fix
                var existing = item.VariantId.HasValue
                    ? await _context.CartItems.FirstOrDefaultAsync(
                        ci => ci.CartId == item.CartId && ci.ProductId == item.ProductId && ci.VariantId == item.VariantId.Value)
                    : await _context.CartItems.FirstOrDefaultAsync(
                        ci => ci.CartId == item.CartId && ci.ProductId == item.ProductId && ci.VariantId == null);

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

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(FormatEfError(ex, "SaveCartItemsAsync failed"));
                throw;
            }
        }

        public async Task AddToCartAsync(int productId, int? variantId, int quantity)
        {
            if (quantity <= 0)
                throw new InvalidOperationException("Số lượng không hợp lệ.");

            var accountId = GetAccountId();
            var sessionId = accountId.HasValue ? null : GetGuestSessionId(true);

            // Validate variant exists if provided
            if (variantId.HasValue)
            {
                var variantOk = await _context.ProductVariants
                    .AnyAsync(v => v.VariantId == variantId && v.ProductId == productId);
                if (!variantOk)
                    throw new InvalidOperationException("Biến thể không hợp lệ hoặc không thuộc sản phẩm này.");
            }

            var cart = await GetOrCreateCartAsync(accountId, sessionId);

            // NULL=NULL fix: separate queries for null vs non-null VariantId
            CartItem? item;
            if (variantId.HasValue)
            {
                item = await _context.CartItems.FirstOrDefaultAsync(
                    ci => ci.CartId == cart.CartId && ci.ProductId == productId && ci.VariantId == variantId.Value);
            }
            else
            {
                item = await _context.CartItems.FirstOrDefaultAsync(
                    ci => ci.CartId == cart.CartId && ci.ProductId == productId && ci.VariantId == null);
            }

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

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log chi tiết để debug
                Console.WriteLine(FormatEfError(ex, $"AddToCartAsync(productId={productId}, variantId={variantId}, qty={quantity})"));
                Console.WriteLine($"  CartId={cart.CartId}, AccountId={accountId}, SessionId={sessionId}");

                // Determine user-friendly message
                var innerMsg = GetInnermostMessage(ex);

                if (innerMsg.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                    || innerMsg.Contains("unique", StringComparison.OrdinalIgnoreCase)
                    || innerMsg.Contains("trùng", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Sản phẩm này đã có trong giỏ hàng rồi. Vui lòng kiểm tra giỏ hàng.");
                }

                if (innerMsg.Contains("foreign key", StringComparison.OrdinalIgnoreCase)
                    || innerMsg.Contains("reference", StringComparison.OrdinalIgnoreCase))
                {
                    if (innerMsg.Contains("ProductId", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Sản phẩm không tồn tại. Vui lòng tải lại trang.");
                    if (innerMsg.Contains("VariantId", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Biến thể không tồn tại. Vui lòng chọn biến thể khác.");
                    if (innerMsg.Contains("CartId", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Lỗi giỏ hàng. Vui lòng tải lại trang.");
                }

                if (innerMsg.Contains("null", StringComparison.OrdinalIgnoreCase)
                    && (innerMsg.Contains("cannot", StringComparison.OrdinalIgnoreCase)
                        || innerMsg.Contains("not null", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException("Dữ liệu không hợp lệ. Vui lòng thử lại.");
                }

                // Generic: re-throw with context but don't expose internals to client
                throw new InvalidOperationException(
                    "Không thể thêm vào giỏ hàng. Lỗi: " + innerMsg, ex);
            }
        }

        public async Task UpdateQuantityAsync(int productId, int? variantId, int quantity)
        {
            var accountId = GetAccountId();
            var sessionId = accountId.HasValue ? null : GetGuestSessionId(false);
            var cart = await GetCartAsync(accountId, sessionId);
            if (cart == null) return;

            // NULL=NULL fix
            CartItem? item;
            if (variantId.HasValue)
            {
                item = await _context.CartItems.FirstOrDefaultAsync(
                    ci => ci.CartId == cart.CartId && ci.ProductId == productId && ci.VariantId == variantId.Value);
            }
            else
            {
                item = await _context.CartItems.FirstOrDefaultAsync(
                    ci => ci.CartId == cart.CartId && ci.ProductId == productId && ci.VariantId == null);
            }

            if (item == null) return;

            if (quantity > 0)
            {
                item.Quantity = quantity;
            }
            else
            {
                _context.CartItems.Remove(item);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(FormatEfError(ex, $"UpdateQuantityAsync(p={productId}, v={variantId}, qty={quantity})"));
                throw;
            }
        }

        public async Task RemoveFromCartAsync(int productId, int? variantId)
        {
            var accountId = GetAccountId();
            var sessionId = accountId.HasValue ? null : GetGuestSessionId(false);
            var cart = await GetCartAsync(accountId, sessionId);
            if (cart == null) return;

            // NULL=NULL fix
            CartItem? item;
            if (variantId.HasValue)
            {
                item = await _context.CartItems.FirstOrDefaultAsync(
                    ci => ci.CartId == cart.CartId && ci.ProductId == productId && ci.VariantId == variantId.Value);
            }
            else
            {
                item = await _context.CartItems.FirstOrDefaultAsync(
                    ci => ci.CartId == cart.CartId && ci.ProductId == productId && ci.VariantId == null);
            }

            if (item != null)
            {
                _context.CartItems.Remove(item);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(FormatEfError(ex, $"RemoveFromCartAsync(p={productId}, v={variantId})"));
                    throw;
                }
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
                // NULL=NULL fix
                CartItem? existing;
                if (item.VariantId.HasValue)
                {
                    existing = await _context.CartItems.FirstOrDefaultAsync(
                        ci => ci.CartId == userCart.CartId && ci.ProductId == item.ProductId && ci.VariantId == item.VariantId.Value);
                }
                else
                {
                    existing = await _context.CartItems.FirstOrDefaultAsync(
                        ci => ci.CartId == userCart.CartId && ci.ProductId == item.ProductId && ci.VariantId == null);
                }

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
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(FormatEfError(ex, "MergeGuestCartAsync failed"));
                throw;
            }

            if (HttpContext != null)
            {
                HttpContext.Response.Cookies.Delete(GuestCartCookie);
            }
        }
    }
}

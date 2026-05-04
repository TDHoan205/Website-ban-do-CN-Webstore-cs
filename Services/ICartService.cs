using Webstore.Models;

namespace Webstore.Services
{
    public interface ICartService
    {
        List<CartItem> GetCartItems(); // Still needed for some synchronous checks
        Task<List<CartItem>> GetCartItemsAsync();
        Task SaveCartItems(List<CartItem> cartItems);
        Task SaveCartItemsAsync(List<CartItem> cartItems);
        Task AddToCartAsync(int productId, int? variantId, int quantity);
        Task UpdateQuantityAsync(int productId, int? variantId, int quantity);
        Task RemoveFromCartAsync(int productId, int? variantId);
        Task ClearCart();
        Task<int> GetCartCount();
        Task MergeGuestCartAsync(int accountId);
    }
}

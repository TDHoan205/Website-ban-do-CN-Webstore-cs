using Webstore.Models;

namespace Webstore.Services
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(PlaceOrderRequest request, int? accountId, List<CartItem>? preloadedCartItems = null);
        Task<IEnumerable<Order>> GetOrderHistory(int accountId);
        Task<IEnumerable<Order>> GetOrderHistoryAsync(int accountId);
        Task<Order?> GetOrderDetails(int orderId, int accountId);
        Task<Order?> GetOrderDetailsAsync(int orderId, int accountId);
        Task ConfirmPaymentAsync(int orderId);
        Task RemoveOrderedItemsFromSessionCartAsync(int orderId);
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<IEnumerable<Order>> GetOrdersByStatusAsync(string status);
        Task<int> GetPendingPaymentCountAsync();
        Task UpdateOrderStatusAsync(int orderId, string status);
    }
}

using Webstore.Models;

namespace Webstore.Services
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(PlaceOrderRequest request, int? accountId);
        Task<IEnumerable<Order>> GetOrderHistory(int accountId);
        Task<IEnumerable<Order>> GetOrderHistoryAsync(int accountId);
        Task<Order?> GetOrderDetails(int orderId, int accountId);
        Task<Order?> GetOrderDetailsAsync(int orderId, int accountId);
        Task ConfirmPaymentAsync(int orderId);
        Task RemoveOrderedItemsFromSessionCartAsync(int orderId);
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task UpdateOrderStatusAsync(int orderId, string status);
    }
}

using Microsoft.EntityFrameworkCore;
using Webstore.Data;
using Webstore.Models;
using Webstore.Data.Repositories;

namespace Webstore.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IRepository<OrderItem> _orderItemRepo;
        private readonly ICartService _cartService;

        public OrderService(IOrderRepository orderRepo, IRepository<OrderItem> orderItemRepo, ICartService cartService)
        {
            _orderRepo = orderRepo;
            _orderItemRepo = orderItemRepo;
            _cartService = cartService;
        }

        public async Task<Order> CreateOrderAsync(PlaceOrderRequest request, int? accountId)
        {
            var cartItems = _cartService.GetCartItems();

            if (request.SelectedProductIds != null && request.SelectedProductIds.Any())
            {
                if (request.SelectedVariantIds != null && request.SelectedVariantIds.Length == request.SelectedProductIds.Length)
                {
                    var filtered = new List<CartItem>();
                    for (int i = 0; i < request.SelectedProductIds.Length; i++)
                    {
                        var pId = request.SelectedProductIds[i];
                        var vId = request.SelectedVariantIds[i] == 0 ? (int?)null : request.SelectedVariantIds[i];
                        var match = cartItems.FirstOrDefault(c => c.ProductId == pId && c.VariantId == vId);
                        if (match != null) filtered.Add(match);
                    }
                    cartItems = filtered;
                }
                else
                {
                    cartItems = cartItems.Where(c => request.SelectedProductIds.Contains(c.ProductId)).ToList();
                }
            }

            if (!cartItems.Any())
            {
                throw new Exception("Không có sản phẩm nào để đặt hàng.");
            }

            var order = new Order
            {
                AccountId = accountId ?? 0, // Fallback if needed, though usually handled by auth
                OrderDate = DateTime.Now,
                Status = "Pending",
                CustomerName = request.CustomerName.Trim(),
                CustomerPhone = request.CustomerPhone.Trim(),
                CustomerAddress = request.CustomerAddress.Trim(),
                Notes = request.Notes?.Trim(),
                TotalAmount = cartItems.Sum(c => {
                    var price = c.VariantId.HasValue && c.Variant != null ? c.Variant.Price : (c.Product?.Price ?? 0m);
                    return price * c.Quantity;
                })
            };

            await _orderRepo.AddAsync(order);
            await _orderRepo.SaveChangesAsync();

            foreach (var item in cartItems)
            {
                decimal unitPrice = item.VariantId.HasValue && item.Variant != null ? item.Variant.Price : (item.Product?.Price ?? 0m);
                
                await _orderItemRepo.AddAsync(new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    VariantId = item.VariantId,
                    Quantity = item.Quantity,
                    UnitPrice = Math.Round(unitPrice, 2)
                });
            }

            await _orderItemRepo.SaveChangesAsync();
            return order;
        }

        public Task<IEnumerable<Order>> GetOrderHistory(int accountId)
        {
            return GetOrderHistoryAsync(accountId);
        }

        public async Task<IEnumerable<Order>> GetOrderHistoryAsync(int accountId)
        {
            return await _orderRepo.FindAsync(o => o.AccountId == accountId);
        }

        public async Task<Order?> GetOrderDetailsAsync(int orderId, int accountId)
        {
            var orders = await _orderRepo.FindAsync(o => o.OrderId == orderId && o.AccountId == accountId);
            return orders.FirstOrDefault();
        }

        public Task<Order?> GetOrderDetails(int orderId, int accountId)
        {
            return GetOrderDetailsAsync(orderId, accountId);
        }

        public async Task ConfirmPaymentAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order != null)
            {
                order.Status = "Paid";
                _orderRepo.Update(order);
                await _orderRepo.SaveChangesAsync();
                await RemoveOrderedItemsFromSessionCartAsync(orderId);
            }
        }

        public async Task RemoveOrderedItemsFromSessionCartAsync(int orderId)
        {
            var items = await _orderItemRepo.FindAsync(oi => oi.OrderId == orderId);
            var orderedItems = items.Select(oi => new { oi.ProductId, oi.VariantId }).ToList();

            if (!orderedItems.Any()) return;

            var cartItems = await _cartService.GetCartItemsAsync();
            cartItems.RemoveAll(c => orderedItems.Any(oi => oi.ProductId == c.ProductId && oi.VariantId == c.VariantId));

            if (cartItems.Any())
            {
                await _cartService.SaveCartItemsAsync(cartItems);
            }
            else
            {
                await _cartService.ClearCart();
            }
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _orderRepo.GetByIdAsync(orderId);
        }

        public async Task UpdateOrderStatusAsync(int orderId, string status)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                _orderRepo.Update(order);
                await _orderRepo.SaveChangesAsync();
            }
        }
    }
}

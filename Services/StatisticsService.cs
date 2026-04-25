using Webstore.Data.Repositories;
using Webstore.Models;
using Microsoft.EntityFrameworkCore;

namespace Webstore.Services
{
    public interface IStatisticsService
    {
        Task<StatisticsViewModel> GetDashboardStatsAsync();
        Task<IEnumerable<Order>> GetRecentOrdersAsync(int count);
        Task<IEnumerable<Product>> GetTopSellingProductsAsync(int count);
    }

    public class StatisticsService : IStatisticsService
    {
        private readonly Webstore.Data.ApplicationDbContext _context;

        public StatisticsService(Webstore.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<StatisticsViewModel> GetDashboardStatsAsync()
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);

            var allOrders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ToListAsync();

            var totalRevenue = allOrders.Where(o => o.Status != "Canceled").Sum(o => o.TotalAmount);
            var todayRevenue = allOrders.Where(o => o.OrderDate.Date == today && o.Status != "Canceled").Sum(o => o.TotalAmount);
            var monthRevenue = allOrders.Where(o => o.OrderDate >= startOfMonth && o.Status != "Canceled").Sum(o => o.TotalAmount);
            var yearRevenue = allOrders.Where(o => o.OrderDate >= startOfYear && o.Status != "Canceled").Sum(o => o.TotalAmount);

            var topProducts = _context.OrderItems
                .Where(oi => oi.Order != null && oi.Order.Status != "Canceled")
                .GroupBy(oi => new { oi.ProductId, oi.Product!.Name, oi.Product.ImageUrl })
                .Select(g => new TopProductViewModel
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    ImageUrl = g.Key.ImageUrl,
                    TotalSold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                })
                .OrderByDescending(p => p.TotalSold)
                .Take(10)
                .ToList();

            var revenueByDay = new List<DailyRevenue>();
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var dayOrders = allOrders.Where(o => o.OrderDate.Date == date && o.Status != "Canceled").ToList();
                revenueByDay.Add(new DailyRevenue { Date = date, Revenue = dayOrders.Sum(o => o.TotalAmount), OrderCount = dayOrders.Count });
            }

            var revenueByMonth = new List<MonthlyRevenue>();
            for (int i = 5; i >= 0; i--)
            {
                var month = today.AddMonths(-i);
                var monthStart = new DateTime(month.Year, month.Month, 1);
                var monthEnd = monthStart.AddMonths(1);
                var mOrders = allOrders.Where(o => o.OrderDate >= monthStart && o.OrderDate < monthEnd && o.Status != "Canceled").ToList();
                revenueByMonth.Add(new MonthlyRevenue
                {
                    Month = month.Month,
                    Year = month.Year,
                    Revenue = mOrders.Sum(o => o.TotalAmount),
                    OrderCount = mOrders.Count,
                    MonthName = month.ToString("MMM yyyy")
                });
            }

            return new StatisticsViewModel
            {
                ProductCount = await _context.Products.CountAsync(),
                OrderCount = allOrders.Count,
                SupplierCount = await _context.Suppliers.CountAsync(),
                AccountCount = await _context.Accounts.CountAsync(),
                InventoryEntries = await _context.Inventory.CountAsync(),
                TotalStock = await _context.Inventory.SumAsync(i => (int?)i.StockQuantity) ?? 0,
                TotalRevenue = totalRevenue,
                TodayRevenue = todayRevenue,
                MonthRevenue = monthRevenue,
                YearRevenue = yearRevenue,
                TodayOrders = allOrders.Count(o => o.OrderDate.Date == today),
                MonthOrders = allOrders.Count(o => o.OrderDate >= startOfMonth),
                TopProducts = topProducts,
                RecentOrders = allOrders.OrderByDescending(o => o.OrderDate).Take(10).ToList(),
                RevenueByDay = revenueByDay,
                RevenueByMonth = revenueByMonth,
                NewOrders = allOrders.Count(o => o.Status == "New"),
                ProcessingOrders = allOrders.Count(o => o.Status == "Processing"),
                ShippedOrders = allOrders.Count(o => o.Status == "Shipped"),
                DeliveredOrders = allOrders.Count(o => o.Status == "Delivered"),
                PendingOrders = allOrders.Count(o => o.Status == "Pending"),
                CompletedOrders = allOrders.Count(o => o.Status == "Completed"),
                CanceledOrders = allOrders.Count(o => o.Status == "Canceled")
            };
        }

        public async Task<IEnumerable<Order>> GetRecentOrdersAsync(int count)
        {
            return await _context.Orders.Include(o => o.Account).OrderByDescending(o => o.OrderDate).Take(count).ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetTopSellingProductsAsync(int count)
        {
            var topIds = await _context.OrderItems
                .GroupBy(oi => oi.ProductId)
                .OrderByDescending(g => g.Sum(oi => oi.Quantity))
                .Select(g => g.Key)
                .Take(count)
                .ToListAsync();

            return await _context.Products.Where(p => topIds.Contains(p.ProductId)).ToListAsync();
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webstore.Data;
using Webstore.Models;

namespace Webstore.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StatisticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? period = "month")
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);

            // Get all orders with items
            var allOrders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ToListAsync();

            // Calculate totals
            var totalRevenue = allOrders
                .Where(o => o.Status != "Canceled")
                .Sum(o => o.TotalAmount);

            var completedOrders = allOrders.Where(o => o.Status == "Completed").ToList();
            var completedRevenue = completedOrders.Sum(o => o.TotalAmount);

            var pendingOrders = allOrders.Count(o => o.Status == "Pending");
            var completedCount = allOrders.Count(o => o.Status == "Completed");
            var canceledCount = allOrders.Count(o => o.Status == "Canceled");
            var processingCount = allOrders.Count(o => o.Status == "Processing");
            var shippedCount = allOrders.Count(o => o.Status == "Shipped");
            var deliveredCount = allOrders.Count(o => o.Status == "Delivered");
            var newCount = allOrders.Count(o => o.Status == "New");

            // Today's revenue
            var todayOrders = allOrders.Where(o => o.OrderDate.Date == today && o.Status != "Canceled").ToList();
            var todayRevenue = todayOrders.Sum(o => o.TotalAmount);

            // This month
            var monthOrders = allOrders.Where(o => o.OrderDate >= startOfMonth && o.Status != "Canceled").ToList();
            var monthRevenue = monthOrders.Sum(o => o.TotalAmount);

            // This year
            var yearOrders = allOrders.Where(o => o.OrderDate >= startOfYear && o.Status != "Canceled").ToList();
            var yearRevenue = yearOrders.Sum(o => o.TotalAmount);

            // Top selling products
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

            // Revenue by day (last 7 days)
            var revenueByDay = new List<DailyRevenue>();
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var dayOrders = allOrders.Where(o => o.OrderDate.Date == date && o.Status != "Canceled").ToList();
                revenueByDay.Add(new DailyRevenue
                {
                    Date = date,
                    Revenue = dayOrders.Sum(o => o.TotalAmount),
                    OrderCount = dayOrders.Count
                });
            }

            // Revenue by month (last 6 months)
            var revenueByMonth = new List<MonthlyRevenue>();
            for (int i = 5; i >= 0; i--)
            {
                var month = today.AddMonths(-i);
                var monthStart = new DateTime(month.Year, month.Month, 1);
                var monthEnd = monthStart.AddMonths(1);
                var monthOrdersData = allOrders.Where(o => o.OrderDate >= monthStart && o.OrderDate < monthEnd && o.Status != "Canceled").ToList();
                revenueByMonth.Add(new MonthlyRevenue
                {
                    Month = month.Month,
                    Year = month.Year,
                    Revenue = monthOrdersData.Sum(o => o.TotalAmount),
                    OrderCount = monthOrdersData.Count,
                    MonthName = month.ToString("MMM yyyy")
                });
            }

            // Recent orders
            var recentOrders = allOrders
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToList();

            var model = new StatisticsViewModel
            {
                ProductCount = await _context.Products.CountAsync(),
                OrderCount = allOrders.Count,
                SupplierCount = await _context.Suppliers.CountAsync(),
                AccountCount = await _context.Accounts.CountAsync(),
                InventoryEntries = await _context.Inventory.CountAsync(),
                TotalStock = await _context.Inventory.SumAsync(i => (int?)i.QuantityInStock) ?? 0,

                // Revenue
                TotalRevenue = totalRevenue,
                TodayRevenue = todayRevenue,
                MonthRevenue = monthRevenue,
                YearRevenue = yearRevenue,

                // Order stats
                TodayOrders = todayOrders.Count,
                MonthOrders = monthOrders.Count,
                PendingOrders = pendingOrders,
                CompletedOrders = completedCount,
                CanceledOrders = canceledCount,
                NewOrders = newCount,
                ProcessingOrders = processingCount,
                ShippedOrders = shippedCount,
                DeliveredOrders = deliveredCount,

                // Data for charts
                TopProducts = topProducts,
                RecentOrders = recentOrders,
                RevenueByDay = revenueByDay,
                RevenueByMonth = revenueByMonth
            };

            return View(model);
        }
    }
}

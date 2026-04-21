namespace Webstore.Models
{
    public class StatisticsViewModel
    {
        // Basic counts
        public int ProductCount { get; set; }
        public int OrderCount { get; set; }
        public int TotalOrders => OrderCount;
        public int SupplierCount { get; set; }
        public int AccountCount { get; set; }
        public int InventoryEntries { get; set; }
        public int TotalStock { get; set; }

        // Revenue statistics
        public decimal TotalRevenue { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public decimal YearRevenue { get; set; }
        public int TodayOrders { get; set; }
        public int MonthOrders { get; set; }
        public int PendingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CanceledOrders { get; set; }

        // Top selling products
        public List<TopProductViewModel> TopProducts { get; set; } = new();

        // Recent orders
        public List<Order> RecentOrders { get; set; } = new();

        // Revenue by day (last 7 days)
        public List<DailyRevenue> RevenueByDay { get; set; } = new();

        // Revenue by month (last 6 months)
        public List<MonthlyRevenue> RevenueByMonth { get; set; } = new();

        // Order status breakdown
        public int NewOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
    }

    public class TopProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string? ImageUrl { get; set; }
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class DailyRevenue
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class MonthlyRevenue
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
        public string MonthName { get; set; } = "";
    }
}

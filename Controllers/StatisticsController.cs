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

        public async Task<IActionResult> Index()
        {
            var model = new StatisticsViewModel
            {
                ProductCount = await _context.Products.CountAsync(),
                OrderCount = await _context.Orders.CountAsync(),
                SupplierCount = await _context.Suppliers.CountAsync(),
                AccountCount = await _context.Accounts.CountAsync(),
                InventoryEntries = await _context.Inventory.CountAsync(),
                TotalStock = await _context.Inventory.SumAsync(i => (int?)i.QuantityInStock) ?? 0
            };

            return View(model);
        }
    }
}

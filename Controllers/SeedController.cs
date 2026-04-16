using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Webstore.Data;

namespace Webstore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SeedController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SeedController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var accounts = await _context.Accounts.ToListAsync();
            var categories = await _context.Categories.ToListAsync();
            var suppliers = await _context.Suppliers.ToListAsync();
            var products = await _context.Products.ToListAsync();

            ViewBag.Accounts = accounts;
            ViewBag.Categories = categories;
            ViewBag.Suppliers = suppliers;
            ViewBag.Products = products;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetData()
        {
            try
            {
                // Xóa tất cả dữ liệu
                _context.Accounts.RemoveRange(_context.Accounts);
                _context.Categories.RemoveRange(_context.Categories);
                _context.Suppliers.RemoveRange(_context.Suppliers);
                _context.Products.RemoveRange(_context.Products);
                _context.Inventory.RemoveRange(_context.Inventory);

                await _context.SaveChangesAsync();

                // Thêm lại dữ liệu mẫu
                await SeedData.SeedAsync(_context);

                TempData["Success"] = "Đã thêm tài khoản mẫu thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi thêm tài khoản: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}

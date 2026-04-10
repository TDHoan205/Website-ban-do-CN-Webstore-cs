using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Webstore.Data;
using Webstore.Models;

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

                TempData["Success"] = "Đã reset và thêm lại dữ liệu mẫu thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi reset dữ liệu: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AddSampleAccounts()
        {
            try
            {
                // Kiểm tra xem đã có tài khoản admin chưa
                var adminExists = await _context.Accounts.AnyAsync(a => a.Username == "admin");
                if (!adminExists)
                {
                    var adminAccount = new Account
                    {
                        Username = "admin",
                        FullName = "Quản trị viên",
                        Email = "admin@webstore.com",
                        Phone = "0123456789",
                        Address = "123 Đường ABC, Quận 1, TP.HCM",
                        Role = "Admin",
                        PasswordHash = "admin123" // Plain text password
                    };
                    _context.Accounts.Add(adminAccount);
                }

                var employeeExists = await _context.Accounts.AnyAsync(a => a.Username == "employee");
                if (!employeeExists)
                {
                    var employeeAccount = new Account
                    {
                        Username = "employee",
                        FullName = "Nhân viên",
                        Email = "employee@webstore.com",
                        Phone = "0987654321",
                        Address = "456 Đường XYZ, Quận 2, TP.HCM",
                        Role = "Employee",
                        PasswordHash = "employee123" // Plain text password
                    };
                    _context.Accounts.Add(employeeAccount);
                }

                var customerExists = await _context.Accounts.AnyAsync(a => a.Username == "customer");
                if (!customerExists)
                {
                    var customerAccount = new Account
                    {
                        Username = "customer",
                        FullName = "Khách hàng",
                        Email = "customer@webstore.com",
                        Phone = "0555666777",
                        Address = "789 Đường DEF, Quận 3, TP.HCM",
                        Role = "Customer",
                        PasswordHash = "customer123" // Plain text password
                    };
                    _context.Accounts.Add(customerAccount);
                }

                await _context.SaveChangesAsync();
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

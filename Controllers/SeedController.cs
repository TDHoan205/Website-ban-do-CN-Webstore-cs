using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Webstore.Data;
using Webstore.Models;
using Webstore.Models.AI;
using Webstore.Models.Security;

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
            var employees = await _context.Employees.ToListAsync();
            var orders = await _context.Orders.ToListAsync();
            var faqs = await _context.FAQs.ToListAsync();

            ViewBag.Accounts = accounts;
            ViewBag.Categories = categories;
            ViewBag.Suppliers = suppliers;
            ViewBag.Products = products;
            ViewBag.Employees = employees;
            ViewBag.Orders = orders;
            ViewBag.FAQs = faqs;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetData()
        {
            try
            {
                // Xóa tất cả dữ liệu theo thứ tự để tránh vi phạm ràng buộc khóa ngoại
                _context.ChatMessages.RemoveRange(_context.ChatMessages);
                _context.ChatSessions.RemoveRange(_context.ChatSessions);
                _context.AIConversationLogs.RemoveRange(_context.AIConversationLogs);
                _context.KnowledgeChunks.RemoveRange(_context.KnowledgeChunks);
                _context.OrderItems.RemoveRange(_context.OrderItems);
                _context.Orders.RemoveRange(_context.Orders);
                _context.CartItems.RemoveRange(_context.CartItems);
                _context.ReceiptShipments.RemoveRange(_context.ReceiptShipments);
                _context.Inventory.RemoveRange(_context.Inventory);
                _context.Employees.RemoveRange(_context.Employees);
                _context.FAQs.RemoveRange(_context.FAQs);
                _context.Notifications.RemoveRange(_context.Notifications);
                _context.Products.RemoveRange(_context.Products);
                _context.Categories.RemoveRange(_context.Categories);
                _context.Suppliers.RemoveRange(_context.Suppliers);
                _context.Accounts.RemoveRange(_context.Accounts);
                _context.ProductVariants.RemoveRange(_context.ProductVariants);

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

        [HttpPost]
        public async Task<IActionResult> AddSampleProducts()
        {
            try
            {
                // Check if products already exist
                if (await _context.Products.AnyAsync())
                {
                    TempData["Error"] = "Sản phẩm đã tồn tại. Vui lòng reset dữ liệu trước.";
                    return RedirectToAction("Index");
                }

                // Create categories first
                var categories = new List<Category>
                {
                    new Category { Name = "Điện thoại di động" },
                    new Category { Name = "Laptop & Máy tính" },
                    new Category { Name = "Tablet" },
                    new Category { Name = "Tai nghe & Âm thanh" },
                    new Category { Name = "Phụ kiện điện tử" },
                    new Category { Name = "Thiết bị mạng" },
                    new Category { Name = "Máy ảnh & Quay phim" },
                    new Category { Name = "Linh kiện máy tính" },
                    new Category { Name = "Đồng hồ thông minh" },
                    new Category { Name = "Bàn phím & Chuột" }
                };
                _context.Categories.AddRange(categories);
                await _context.SaveChangesAsync();

                // Create suppliers
                var suppliers = new List<Supplier>
                {
                    new Supplier { Name = "Apple Vietnam", Email = "contact@apple.com.vn", Phone = "1800-1192" },
                    new Supplier { Name = "Samsung Vietnam", Email = "contact@samsung.com.vn", Phone = "1800-588-889" },
                    new Supplier { Name = "Dell Vietnam", Email = "dell@vietnam.com", Phone = "1800-545-455" },
                    new Supplier { Name = "Sony Vietnam", Email = "sony@vietnam.com", Phone = "1800-588-880" },
                    new Supplier { Name = "Xiaomi Vietnam", Email = "xiaomi@vn.com", Phone = "1800-1234-05" }
                };
                _context.Suppliers.AddRange(suppliers);
                await _context.SaveChangesAsync();

                // Create products
                var phoneCat = categories[0];
                var laptopCat = categories[1];
                var tabletCat = categories[2];
                var audioCat = categories[3];
                var accessoryCat = categories[4];
                var watchCat = categories[8];
                var keyboardCat = categories[9];

                var apple = suppliers[0];
                var samsung = suppliers[1];
                var dell = suppliers[2];
                var sony = suppliers[3];
                var xiaomi = suppliers[4];

                var products = new List<Product>
                {
                    // Điện thoại
                    new Product { Name = "iPhone 15 Pro Max 256GB", Price = 34990000, ImageUrl = "/images/products/iPhone_15_Pro_Max.png", CategoryId = phoneCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true, IsHot = true },
                    new Product { Name = "iPhone 15 Pro 128GB", Price = 27990000, ImageUrl = "/images/products/iPhone_15.png", CategoryId = phoneCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true },
                    new Product { Name = "Samsung Galaxy S24 Ultra 5G", Price = 29990000, ImageUrl = "/images/products/Galaxy_S24_Ultra.png", CategoryId = phoneCat.CategoryId, SupplierId = samsung.SupplierId, IsNew = true, IsHot = true },
                    new Product { Name = "Samsung Galaxy Z Flip5", Price = 22990000, ImageUrl = "/images/products/Z_Flip5.png", CategoryId = phoneCat.CategoryId, SupplierId = samsung.SupplierId, IsHot = true },
                    new Product { Name = "Xiaomi 13T Pro", Price = 19990000, ImageUrl = "/images/products/Xiaomi_13T_Pro.png", CategoryId = phoneCat.CategoryId, SupplierId = xiaomi.SupplierId, IsNew = true },
                    new Product { Name = "iPhone 13 128GB", Price = 15990000, ImageUrl = "/images/products/iPhone_13.png", CategoryId = phoneCat.CategoryId, SupplierId = apple.SupplierId, IsHot = true },
                    new Product { Name = "Samsung Galaxy A55 5G", Price = 9990000, ImageUrl = "/images/products/Galaxy_A55_5G.png", CategoryId = phoneCat.CategoryId, SupplierId = samsung.SupplierId },
                    new Product { Name = "Samsung Galaxy A35 5G", Price = 7490000, ImageUrl = "/images/products/Galaxy_A35_5G.png", CategoryId = phoneCat.CategoryId, SupplierId = samsung.SupplierId },
                    new Product { Name = "Xiaomi Redmi Note 13 Pro", Price = 7990000, ImageUrl = "/images/products/Redmi_Note_13_Pro.png", CategoryId = phoneCat.CategoryId, SupplierId = xiaomi.SupplierId, IsHot = true },
                    new Product { Name = "Xiaomi POCO X6 Pro", Price = 9990000, ImageUrl = "/images/products/POCO_X6_Pro.png", CategoryId = phoneCat.CategoryId, SupplierId = xiaomi.SupplierId, IsNew = true, IsHot = true },

                    // Laptop
                    new Product { Name = "MacBook Pro 14 M3 Pro", Price = 49990000, ImageUrl = "/images/products/MacBook_Pro_14_M3_Pro.png", CategoryId = laptopCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true, IsHot = true },
                    new Product { Name = "MacBook Air 15 M3", Price = 34990000, ImageUrl = "/images/products/MacBook_Air_M3.png", CategoryId = laptopCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true, IsHot = true },
                    new Product { Name = "Dell XPS 15 9530", Price = 69990000, ImageUrl = "/images/products/Dell_XPS_15.png", CategoryId = laptopCat.CategoryId, SupplierId = dell.SupplierId },
                    new Product { Name = "ASUS ROG Zephyrus G14", Price = 54990000, ImageUrl = "/images/products/ROG_Zephyrus_G14.png", CategoryId = laptopCat.CategoryId, SupplierId = dell.SupplierId, IsHot = true },
                    new Product { Name = "Lenovo ThinkPad X1 Carbon", Price = 49990000, ImageUrl = "/images/products/ThinkPad_X1_Carbon.png", CategoryId = laptopCat.CategoryId, SupplierId = dell.SupplierId },
                    new Product { Name = "HP Pavilion Plus 14", Price = 29990000, ImageUrl = "/images/products/HP_Pavilion_Plus_14.png", CategoryId = laptopCat.CategoryId, SupplierId = dell.SupplierId, IsNew = true },
                    new Product { Name = "HP Victus 15", Price = 18990000, ImageUrl = "/images/products/HP_Victus_15.png", CategoryId = laptopCat.CategoryId, SupplierId = dell.SupplierId, IsHot = true },
                    new Product { Name = "ASUS ZenBook 14 OLED", Price = 27990000, ImageUrl = "/images/products/ZenBook_14_OLED.png", CategoryId = laptopCat.CategoryId, SupplierId = dell.SupplierId, IsNew = true, IsHot = true },
                    new Product { Name = "MSI Modern 15 H", Price = 24990000, ImageUrl = "/images/products/MSI_Modern_15_H.png", CategoryId = laptopCat.CategoryId, SupplierId = dell.SupplierId },
                    new Product { Name = "Acer Swift Go 14", Price = 21990000, ImageUrl = "/images/products/Acer_Swift_Go_14.png", CategoryId = laptopCat.CategoryId, SupplierId = dell.SupplierId, IsNew = true },

                    // Tablet
                    new Product { Name = "iPad Pro 12.9 M2", Price = 32990000, ImageUrl = "/images/products/iPad_Pro_12.9.png", CategoryId = tabletCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true },
                    new Product { Name = "iPad Air M2", Price = 22990000, ImageUrl = "/images/products/iPad_Air_M2.png", CategoryId = tabletCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true, IsHot = true },
                    new Product { Name = "Samsung Tab S9 Ultra", Price = 28990000, ImageUrl = "/images/products/Tab_S9_Ultra.png", CategoryId = tabletCat.CategoryId, SupplierId = samsung.SupplierId, IsNew = true, IsHot = true },
                    new Product { Name = "iPad mini 6", Price = 14990000, ImageUrl = "/images/products/iPad_mini_6.png", CategoryId = tabletCat.CategoryId, SupplierId = apple.SupplierId },
                    new Product { Name = "Samsung Tab S9 FE", Price = 9990000, ImageUrl = "/images/products/Tab_S9_FE.png", CategoryId = tabletCat.CategoryId, SupplierId = samsung.SupplierId },
                    new Product { Name = "Xiaomi Pad 6", Price = 8990000, ImageUrl = "/images/products/Xiaomi_Pad_6.png", CategoryId = tabletCat.CategoryId, SupplierId = xiaomi.SupplierId, IsHot = true },
                    new Product { Name = "Huawei MatePad 11.5", Price = 7990000, ImageUrl = "/images/products/MatePad_11.5.png", CategoryId = tabletCat.CategoryId, SupplierId = xiaomi.SupplierId },
                    new Product { Name = "OPPO Pad Air2", Price = 5990000, ImageUrl = "/images/products/OPPO_Pad_Air2.png", CategoryId = tabletCat.CategoryId, SupplierId = xiaomi.SupplierId },
                    new Product { Name = "Realme Pad X", Price = 6990000, ImageUrl = "/images/products/Realme_Pad_X.png", CategoryId = tabletCat.CategoryId, SupplierId = xiaomi.SupplierId },
                    new Product { Name = "Samsung Tab S9 FE+", Price = 15990000, ImageUrl = "/images/products/Tab_S9_Ultra.png", CategoryId = tabletCat.CategoryId, SupplierId = samsung.SupplierId, IsHot = true },

                    // Tai nghe
                    new Product { Name = "Sony WH-1000XM5", Price = 9990000, ImageUrl = "/images/products/Sony_WH-1000XM5.png", CategoryId = audioCat.CategoryId, SupplierId = sony.SupplierId, IsNew = true, IsHot = true },
                    new Product { Name = "AirPods Pro 2", Price = 6490000, ImageUrl = "/images/products/AirPods_Pro_2.png", CategoryId = audioCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true, IsHot = true },
                    new Product { Name = "Samsung Galaxy Buds2 Pro", Price = 4990000, ImageUrl = "/images/products/Galaxy_A35_5G.png", CategoryId = audioCat.CategoryId, SupplierId = samsung.SupplierId },
                    new Product { Name = "Sony WF-1000XM5", Price = 7490000, ImageUrl = "/images/products/Sony_WH-1000XM5.png", CategoryId = audioCat.CategoryId, SupplierId = sony.SupplierId, IsNew = true },
                    new Product { Name = "JBL Flip 6", Price = 2490000, ImageUrl = "/images/products/Sony_WH-1000XM5.png", CategoryId = audioCat.CategoryId, SupplierId = sony.SupplierId, IsHot = true },

                    // Phụ kiện
                    new Product { Name = "Anker 735 65W GaN", Price = 1290000, ImageUrl = "/images/products/Anker_735_65W.png", CategoryId = accessoryCat.CategoryId, SupplierId = xiaomi.SupplierId, IsNew = true, IsHot = true },
                    new Product { Name = "HyperDrive Gen2", Price = 2490000, ImageUrl = "/images/products/HyperDrive_Gen2.png", CategoryId = accessoryCat.CategoryId, SupplierId = xiaomi.SupplierId, IsHot = true },
                    new Product { Name = "Samsung T7 1TB SSD", Price = 2490000, ImageUrl = "/images/products/Samsung_T7_1TB.png", CategoryId = accessoryCat.CategoryId, SupplierId = samsung.SupplierId, IsHot = true },
                    new Product { Name = "Targus Newport", Price = 990000, ImageUrl = "/images/products/Targus_Newport.png", CategoryId = accessoryCat.CategoryId, SupplierId = xiaomi.SupplierId },
                    new Product { Name = "Anker PowerCore 20000", Price = 1490000, ImageUrl = "/images/products/Anker_735_65W.png", CategoryId = accessoryCat.CategoryId, SupplierId = xiaomi.SupplierId },

                    // Bàn phím & Chuột
                    new Product { Name = "Logitech MX Master 3S", Price = 2490000, ImageUrl = "/images/products/MX_Master_3S.png", CategoryId = keyboardCat.CategoryId, SupplierId = dell.SupplierId, IsNew = true, IsHot = true },
                    new Product { Name = "Keychron K3 Pro", Price = 2290000, ImageUrl = "/images/products/Keychron_K3_Pro.png", CategoryId = keyboardCat.CategoryId, SupplierId = dell.SupplierId },
                    new Product { Name = "Logitech G Pro X Superlight 2", Price = 3490000, ImageUrl = "/images/products/MX_Master_3S.png", CategoryId = keyboardCat.CategoryId, SupplierId = dell.SupplierId, IsNew = true, IsHot = true },
                    new Product { Name = "Corsair K70 RGB Pro", Price = 3990000, ImageUrl = "/images/products/Keychron_K3_Pro.png", CategoryId = keyboardCat.CategoryId, SupplierId = dell.SupplierId },
                    new Product { Name = "Logitech G502 X Plus", Price = 1990000, ImageUrl = "/images/products/MX_Master_3S.png", CategoryId = keyboardCat.CategoryId, SupplierId = dell.SupplierId, IsHot = true }
                };

                _context.Products.AddRange(products);
                await _context.SaveChangesAsync();

                // Create inventory for all products
                var inventories = products.Select((p, index) => new Inventory
                {
                    ProductId = p.ProductId,
                    StockQuantity = new Random(index).Next(10, 200),
                    LastUpdated = DateTime.Now
                }).ToList();
                _context.Inventory.AddRange(inventories);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Đã thêm {products.Count} sản phẩm mẫu thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi thêm sản phẩm: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}

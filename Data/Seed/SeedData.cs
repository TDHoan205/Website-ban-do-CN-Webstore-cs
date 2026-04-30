using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Webstore.Models;
using Webstore.Models.AI;
using Webstore.Models.Security;
using Webstore.Utilities;

namespace Webstore.Data
{
    public static class SeedData
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Reset dữ liệu cũ theo thứ tự chuẩn để tránh lỗi Foreign Key
            await context.Database.ExecuteSqlRawAsync("DELETE FROM OrderDetails");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM Orders");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM ProductVariants");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM Inventory");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM Products");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM FAQs");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM Suppliers");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM Categories");
            
            // Xóa dữ liệu AI/Chat trước khi xóa Accounts
            await context.Database.ExecuteSqlRawAsync("IF OBJECT_ID('AIConversationLogs', 'U') IS NOT NULL DELETE FROM AIConversationLogs");
            await context.Database.ExecuteSqlRawAsync("IF OBJECT_ID('ChatMessages', 'U') IS NOT NULL DELETE FROM ChatMessages");
            await context.Database.ExecuteSqlRawAsync("IF OBJECT_ID('ChatSessions', 'U') IS NOT NULL DELETE FROM ChatSessions");
            await context.Database.ExecuteSqlRawAsync("IF OBJECT_ID('Notifications', 'U') IS NOT NULL DELETE FROM Notifications");

            await context.Database.ExecuteSqlRawAsync("DELETE FROM Accounts");

            // Hash passwords properly: salt:hash format
            string HashPass(string p)
            {
                var salt = PasswordHasher.GenerateSalt();
                return salt + ":" + PasswordHasher.HashPassword(p, salt);
            }

            // ========== TÀI KHOẢN (20 mẫu) ==========
            var accounts = new List<Account>
            {
                new Account { Username = "admin", FullName = "Nguyễn Văn An", Email = "admin@webstore.com", Phone = "0123456789", Address = "123 Đường ABC, Quận 1, TP.HCM", Role = "Admin", PasswordHash = HashPass("admin123"), IsActive = true },
                new Account { Username = "employee", FullName = "Trần Thị Bình", Email = "employee@webstore.com", Phone = "0987654321", Address = "456 Đường XYZ, Quận 2, TP.HCM", Role = "Employee", PasswordHash = HashPass("employee123"), IsActive = true },
                new Account { Username = "khachhang1", FullName = "Lê Minh Cường", Email = "cuong.le@email.com", Phone = "0901234567", Address = "789 Đường DEF, Quận 3, TP.HCM", Role = "Customer", PasswordHash = HashPass("password123"), IsActive = true },
                new Account { Username = "khachhang2", FullName = "Phạm Hoàng Duy", Email = "duy.pham@email.com", Phone = "0902345678", Address = "321 Đường GHI, Quận 4, TP.HCM", Role = "Customer", PasswordHash = HashPass("password123"), IsActive = true },
                new Account { Username = "khachhang3", FullName = "Vũ Thị Em", Email = "em.vu@email.com", Phone = "0903456789", Address = "654 Đường JKL, Quận 5, TP.HCM", Role = "Customer", PasswordHash = HashPass("password123"), IsActive = true },
                new Account { Username = "khachhang4", FullName = "Đặng Minh Phong", Email = "phong.dang@email.com", Phone = "0904567890", Address = "987 Đường MNO, Quận 6, TP.HCM", Role = "Customer", PasswordHash = HashPass("password123"), IsActive = true },
                new Account { Username = "khachhang5", FullName = "Bùi Thị Quỳnh", Email = "quynh.bui@email.com", Phone = "0905678901", Address = "147 Đường PQR, Quận 7, TP.HCM", Role = "Customer", PasswordHash = HashPass("password123"), IsActive = true },
                new Account { Username = "khachhang6", FullName = "Hoàng Văn Sơn", Email = "son.hoang@email.com", Phone = "0906789012", Address = "258 Đường STU, Quận 8, TP.HCM", Role = "Customer", PasswordHash = HashPass("password123"), IsActive = true },
                new Account { Username = "khachhang7", FullName = "Ngô Thị Thanh", Email = "thanh.ngo@email.com", Phone = "0907890123", Address = "369 Đường VWX, Quận 9, TP.HCM", Role = "Customer", PasswordHash = HashPass("password123"), IsActive = true },
                new Account { Username = "khachhang8", FullName = "Trịnh Văn Tùng", Email = "tung.trinh@email.com", Phone = "0908901234", Address = "741 Đường YZA, Quận 10, TP.HCM", Role = "Customer", PasswordHash = HashPass("password123"), IsActive = true },
                new Account { Username = "khachhang9", FullName = "Lý Thị Vân", Email = "van.ly@email.com", Phone = "0909012345", Address = "852 Đường BCD, Quận 11, TP.HCM", Role = "Customer", PasswordHash = HashPass("password123"), IsActive = true },
                new Account { Username = "khachhang10", FullName = "Đinh Minh Tuấn", Email = "tuan.dinh@email.com", Phone = "0910123456", Address = "963 Đường EFG, Quận 12, TP.HCM", Role = "Customer", PasswordHash = HashPass("password123"), IsActive = true },
                new Account { Username = "nhanvien1", FullName = "Cao Thị Hương", Email = "huong.cao@webstore.com", Phone = "0911234567", Address = "159 Đường HIJ, Quận Bình Thạnh, TP.HCM", Role = "Employee", PasswordHash = HashPass("employee123"), IsActive = true },
                new Account { Username = "nhanvien2", FullName = "Bạch Văn Kiên", Email = "kien.bach@webstore.com", Phone = "0912345678", Address = "753 Đường KLM, Quận Gò Vấp, TP.HCM", Role = "Employee", PasswordHash = HashPass("employee123"), IsActive = true },
                new Account { Username = "nhanvien3", FullName = "Tạ Thị Lan", Email = "lan.ta@webstore.com", Phone = "0913456789", Address = "951 Đường NOP, Quận Phú Nhuận, TP.HCM", Role = "Employee", PasswordHash = HashPass("employee123"), IsActive = true },
                new Account { Username = "nhanvien4", FullName = "Phùng Văn Mạnh", Email = "manh.phung@webstore.com", Phone = "0914567890", Address = "357 Đường QRS, Quận Tân Bình, TP.HCM", Role = "Employee", PasswordHash = HashPass("employee123"), IsActive = true },
                new Account { Username = "khachhang11", FullName = "Võ Thị Ngọc", Email = "ngoc.vo@email.com", Phone = "0915678901", Address = "159 Đường TUV, Quận Tân Phú, TP.HCM", Role = "Customer", PasswordHash = HashPass("password123"), IsActive = true },
                new Account { Username = "khachhang12", FullName = "Đỗ Văn Hùng", Email = "hung.do@email.com", Phone = "0916789012", Address = "753 Đường WXY, Quận Bình Tân, TP.HCM", Role = "Customer", PasswordHash = HashPass("password123"), IsActive = true },
                new Account { Username = "khachhang13", FullName = "Hồ Thị Mai", Email = "mai.ho@email.com", Phone = "0917890123", Address = "951 Đường ZAB, Huyện Hóc Môn, TP.HCM", Role = "Customer", PasswordHash = HashPass("password123"), IsActive = true },
                new Account { Username = "khachhang14", FullName = "Nguyễn Văn Đức", Email = "duc.nguyen@email.com", Phone = "0918901234", Address = "246 Đường CDE, Huyện Củ Chi, TP.HCM", Role = "Customer", PasswordHash = HashPass("password123"), IsActive = true }
            };
            try
            {
                context.Accounts.AddRange(accounts);
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"❌ Lỗi khi lưu Accounts: {ex.InnerException?.Message ?? ex.Message}");
                throw;
            }

            // ========== DANH MỤC (20 mẫu) ==========
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
                new Category { Name = "Bàn phím & Chuột" },
                new Category { Name = "Màn hình máy tính" },
                new Category { Name = "Ổ cứng & Lưu trữ" },
                new Category { Name = "Sạc & Cáp kết nối" },
                new Category { Name = "Bảo vệ & Ốp lưng" },
                new Category { Name = "Máy in & Thiết bị văn phòng" },
                new Category { Name = "Camera giám sát" },
                new Category { Name = "Loa & Dàn âm thanh" },
                new Category { Name = "Gaming Gear" },
                new Category { Name = "Thiết bị thông minh" },
                new Category { Name = "Phần mềm & Bản quyền" }
            };
            try
            {
                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"❌ Lỗi khi lưu Categories: {ex.InnerException?.Message ?? ex.Message}");
                throw;
            }

            // ========== NHÀ CUNG CẤP (20 mẫu) ==========
            // Cleanup corrupted suppliers
            var corruptedSuppliers = await context.Suppliers
                .Where(s => s.Name.Contains("") || s.Name.Contains("?"))
                .ToListAsync();
            if (corruptedSuppliers.Any())
            {
                context.Suppliers.RemoveRange(corruptedSuppliers);
                await context.SaveChangesAsync();
            }

            var suppliers = new List<Supplier>
            {
                new Supplier { Name = "Apple Vietnam", Email = "contact@apple.com.vn", Phone = "1800-1192", ContactPerson = "Phòng Kinh Doanh", Address = "Lầu 3, Sheraton Plaza, 175 Đồng Khởi, Q.1, TP.HCM" },
                new Supplier { Name = "Samsung Electronics Vietnam", Email = "contact@samsung.com.vn", Phone = "1800-588-889", ContactPerson = "Bộ phận Đối tác Bán lẻ", Address = "Tòa nhà PVI, 1 Phạm Văn Bạch, Cầu Giấy, Hà Nội" },
                new Supplier { Name = "Dell Vietnam", Email = "dell@vietnam.com", Phone = "1800-545-455", ContactPerson = "Phòng Phân phối", Address = "Tòa nhà Empress, 128-128 Bis Hồng Bàng, Q.5, TP.HCM" },
                new Supplier { Name = "Sony Vietnam", Email = "sony@vietnam.com", Phone = "1800-588-880", ContactPerson = "Phòng Kinh doanh", Address = "Tòa nhà Vietnam Business Center, Q.1, TP.HCM" },
                new Supplier { Name = "LG Electronics Vietnam", Email = "lg@vietnam.com", Phone = "1800-1503", ContactPerson = "Bộ phận Đối tác", Address = "Tòa nhà Keangnam Landmark, Phạm Hùng, Hà Nội" },
                new Supplier { Name = "Xiaomi Vietnam", Email = "xiaomi@vn.com", Phone = "1800-1234-05", ContactPerson = "Phòng Marketing", Address = "Tòa nhà Bitexco, Q.1, TP.HCM" },
                new Supplier { Name = "OPPO Vietnam", Email = "oppo@vn.com", Phone = "1800-5555-01", ContactPerson = "Bộ phận Kinh doanh", Address = "Tòa nhà Lotte Center, Q.1, TP.HCM" },
                new Supplier { Name = "Realme Vietnam", Email = "realme@vn.com", Phone = "1800-8888-01", ContactPerson = "Phòng Hỗ trợ", Address = "Quận 7, TP.HCM" },
                new Supplier { Name = "ASUS Vietnam", Email = "asus@vn.com", Phone = "1800-8888-09", ContactPerson = "Phòng Kinh doanh", Address = "Tòa nhà Saigon Tower, Q.1, TP.HCM" },
                new Supplier { Name = "HP Vietnam", Email = "hp@vn.com", Phone = "1800-5888-54", ContactPerson = "Bộ phận Bán lẻ", Address = "Tòa nhà Gemadept, Q.1, TP.HCM" },
                new Supplier { Name = "Lenovo Vietnam", Email = "lenovo@vn.com", Phone = "1800-1003", ContactPerson = "Phòng Kinh doanh", Address = "Quận 2, TP.HCM" },
                new Supplier { Name = "Logitech Vietnam", Email = "logitech@vn.com", Phone = "1800-1234-89", ContactPerson = "Bộ phận Phân phối", Address = "Quận Phú Nhuận, TP.HCM" },
                new Supplier { Name = "JBL Vietnam", Email = "jbl@vn.com", Phone = "1800-8888-52", ContactPerson = "Phòng Marketing", Address = "Quận 3, TP.HCM" },
                new Supplier { Name = "Anker Vietnam", Email = "anker@vn.com", Phone = "1800-1234-56", ContactPerson = "Bộ phận Hỗ trợ", Address = "Quận Bình Thạnh, TP.HCM" },
                new Supplier { Name = "Kingston Technology", Email = "kingston@vn.com", Phone = "1800-8888-44", ContactPerson = "Phòng Kinh doanh", Address = "Quận Gò Vấp, TP.HCM" },
                new Supplier { Name = "Corsair Vietnam", Email = "corsair@vn.com", Phone = "1800-9999-88", ContactPerson = "Bộ phận Gaming", Address = "Quận Tân Bình, TP.HCM" },
                new Supplier { Name = "Western Digital Vietnam", Email = "wd@vn.com", Phone = "1800-5555-44", ContactPerson = "Phòng Phân phối", Address = "Quận 10, TP.HCM" },
                new Supplier { Name = "TP-Link Vietnam", Email = "tplink@vn.com", Phone = "1800-8888-47", ContactPerson = "Bộ phận Mạng", Address = "Quận Tân Phú, TP.HCM" },
                new Supplier { Name = "Canon Vietnam", Email = "canon@vn.com", Phone = "1800-9999-26", ContactPerson = "Phòng Kinh doanh", Address = "Tòa nhà MB Center, Q.1, TP.HCM" },
                new Supplier { Name = "Nikon Vietnam", Email = "nikon@vn.com", Phone = "1800-1234-65", ContactPerson = "Bộ phận Camera", Address = "Quận 3, TP.HCM" }
            };
            try
            {
                context.Suppliers.AddRange(suppliers);
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"❌ Lỗi khi lưu Suppliers: {ex.InnerException?.Message ?? ex.Message}");
                throw;
            }

            // ========== SẢN PHẨM (50 mẫu với ảnh thực) ==========
            var phoneCat = await context.Categories.FirstAsync(c => c.Name == "Điện thoại di động");
            var laptopCat = await context.Categories.FirstAsync(c => c.Name == "Laptop & Máy tính");
            var tabletCat = await context.Categories.FirstAsync(c => c.Name == "Tablet");
            var audioCat = await context.Categories.FirstAsync(c => c.Name == "Tai nghe & Âm thanh");
            var accessoryCat = await context.Categories.FirstAsync(c => c.Name == "Phụ kiện điện tử");
            var networkCat = await context.Categories.FirstAsync(c => c.Name == "Thiết bị mạng");
            var cameraCat = await context.Categories.FirstAsync(c => c.Name == "Máy ảnh & Quay phim");
            var componentCat = await context.Categories.FirstAsync(c => c.Name == "Linh kiện máy tính");
            var keyboardCat = await context.Categories.FirstAsync(c => c.Name == "Bàn phím & Chuột");
            var storageCat = await context.Categories.FirstAsync(c => c.Name == "Ổ cứng & Lưu trữ");
            var cableCat = await context.Categories.FirstAsync(c => c.Name == "Sạc & Cáp kết nối");
            var watchCat = await context.Categories.FirstAsync(c => c.Name == "Đồng hồ thông minh");

            var apple = await context.Suppliers.FirstAsync(s => s.Name == "Apple Vietnam");
            var samsung = await context.Suppliers.FirstAsync(s => s.Name == "Samsung Electronics Vietnam");
            var dell = await context.Suppliers.FirstAsync(s => s.Name == "Dell Vietnam");
            var sony = await context.Suppliers.FirstAsync(s => s.Name == "Sony Vietnam");
            var lg = await context.Suppliers.FirstAsync(s => s.Name == "LG Electronics Vietnam");
            var xiaomi = await context.Suppliers.FirstAsync(s => s.Name == "Xiaomi Vietnam");
            var oppo = await context.Suppliers.FirstAsync(s => s.Name == "OPPO Vietnam");
            var realme = await context.Suppliers.FirstAsync(s => s.Name == "Realme Vietnam");
            var asus = await context.Suppliers.FirstAsync(s => s.Name == "ASUS Vietnam");
            var hp = await context.Suppliers.FirstAsync(s => s.Name == "HP Vietnam");
            var lenovo = await context.Suppliers.FirstAsync(s => s.Name == "Lenovo Vietnam");
            var logitech = await context.Suppliers.FirstAsync(s => s.Name == "Logitech Vietnam");
            var jbl = await context.Suppliers.FirstAsync(s => s.Name == "JBL Vietnam");
            var anker = await context.Suppliers.FirstAsync(s => s.Name == "Anker Vietnam");
            var corsair = await context.Suppliers.FirstAsync(s => s.Name == "Corsair Vietnam");

            var products = new List<Product>

            {
                // ========== ĐIỆN THOẠI (10 mẫu) ==========
                new Product { Name = "iPhone 15 Pro Max 256GB Titan Tự Nhiên", Description = "Titan grade 5, A17 Pro chip, camera 48MP, Dynamic Island, pin siêu bền cả ngày.", Price = 34990000, ImageUrl = "/images/products/iPhone_15_Pro_Max.png", CategoryId = phoneCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true, IsHot = true },
                new Product { Name = "iPhone 15 Pro 128GB Titan Xanh", Description = "Chip A17 Pro, camera 48MP, titanium grade 5, USB-C 3.0.", Price = 27990000, ImageUrl = "/images/products/iPhone_15.png", CategoryId = phoneCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true, IsHot = false },
                new Product { Name = "iPhone 15 128GB Xanh Dương", Description = "Chip A16 Bionic, camera 48MP, Dynamic Island, pin 24h.", Price = 19990000, ImageUrl = "/images/products/iPhone_15.png", CategoryId = phoneCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true, IsHot = false },
                new Product { Name = "iPhone 13 128GB Hồng", Description = "Chip A15 Bionic, camera kép 12MP, Face ID nhanh.", Price = 15990000, ImageUrl = "/images/products/iPhone_13.png", CategoryId = phoneCat.CategoryId, SupplierId = apple.SupplierId, IsNew = false, IsHot = true },
                new Product { Name = "Samsung Galaxy S24 Ultra 256GB Titan Đen", Description = "S Pen tích hợp, màn hình Dynamic AMOLED 2X 6.8 inch, camera 200MP, pin 5000mAh.", Price = 29990000, ImageUrl = "/images/products/Galaxy_S24_Ultra.png", CategoryId = phoneCat.CategoryId, SupplierId = samsung.SupplierId, IsNew = true, IsHot = true },
                new Product { Name = "Samsung Galaxy S24+ 256GB Tím", Description = "Màn hình 6.7 inch AMOLED 2X, camera 50MP, pin 4900mAh.", Price = 24990000, ImageUrl = "/images/products/Galaxy_A55_5G.png", CategoryId = phoneCat.CategoryId, SupplierId = samsung.SupplierId, IsNew = true, IsHot = false },
                new Product { Name = "Samsung Galaxy A55 5G 128GB Xanh", Description = "Màn hình 6.6 inch Super AMOLED, camera 50MP, pin 5000mAh.", Price = 9990000, ImageUrl = "/images/products/Galaxy_A55_5G.png", CategoryId = phoneCat.CategoryId, SupplierId = samsung.SupplierId, IsNew = false, IsHot = true },
                new Product { Name = "Samsung Galaxy A35 5G 128GB Vàng", Description = "Màn hình 6.6 inch FHD+, camera 50MP OIS, kháng nước IP67.", Price = 7490000, ImageUrl = "/images/products/Galaxy_A35_5G.png", CategoryId = phoneCat.CategoryId, SupplierId = samsung.SupplierId, IsNew = false, IsHot = false },
                new Product { Name = "Samsung Galaxy Z Flip5 256GB Xanh", Description = "Màn hình gập nhỏ gọn 6.7 inch, Snapdragon 8 Gen 2, Flex Mode đa năng.", Price = 22990000, ImageUrl = "/images/products/Z_Flip5.png", CategoryId = phoneCat.CategoryId, SupplierId = samsung.SupplierId, IsNew = false, IsHot = true },
                new Product { Name = "Xiaomi 13T Pro 512GB Xanh", Description = "Snapdragon 8 Gen 2, Leica camera 50MP, 120W HyperCharge siêu nhanh.", Price = 19990000, ImageUrl = "/images/products/Xiaomi_13T_Pro.png", CategoryId = phoneCat.CategoryId, SupplierId = xiaomi.SupplierId, IsNew = true, IsHot = false },

                // ========== LAPTOP (10 mẫu) ==========
                new Product { Name = "MacBook Pro 14 M3 Pro 512GB Bạc", Description = "Chip M3 Pro 12-core CPU, 18-core GPU, 18GB RAM, 512GB SSD, Liquid Retina XDR.", Price = 49990000, ImageUrl = "/images/products/MacBook_Pro_14_M3_Pro.png", CategoryId = laptopCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true, IsHot = true },
                new Product { Name = "MacBook Air 15 M3 256GB Xám", Description = "Chip M3, 15.3 inch Liquid Retina, 8GB RAM, 256GB SSD, pin 18h.", Price = 32990000, ImageUrl = "/images/products/MacBook_Air_M3.png", CategoryId = laptopCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true, IsHot = true },
                new Product { Name = "Dell XPS 15 9530 1TB Bạc", Description = "Intel Core i9-13900H, RTX 4070, 32GB RAM, 1TB SSD, 15.6 inch 3.5K OLED.", Price = 69990000, ImageUrl = "/images/products/Dell_XPS_15.png", CategoryId = laptopCat.CategoryId, SupplierId = dell.SupplierId, IsNew = false, IsHot = false },
                new Product { Name = "Dell Inspiron 15 3530 512GB Đen", Description = "Intel Core i5-1335U, 8GB RAM, 512GB SSD, 15.6 inch FHD IPS.", Price = 15990000, ImageUrl = "/images/products/Inspiron_15_3530.png", CategoryId = laptopCat.CategoryId, SupplierId = dell.SupplierId, IsNew = false, IsHot = false },
                new Product { Name = "ASUS ROG Zephyrus G14 1TB Trắng", Description = "AMD Ryzen 9 7940HS, RTX 4070, 16GB RAM, 1TB SSD, 14 inch 165Hz.", Price = 54990000, ImageUrl = "/images/products/ROG_Zephyrus_G14.png", CategoryId = laptopCat.CategoryId, SupplierId = asus.SupplierId, IsNew = false, IsHot = true },
                new Product { Name = "ASUS VivoBook 15 512GB Bạc", Description = "Intel Core i5-1235U, 8GB RAM, 512GB SSD, 15.6 inch FHD.", Price = 13990000, ImageUrl = "/images/products/VivoBook_15.png", CategoryId = laptopCat.CategoryId, SupplierId = asus.SupplierId, IsNew = false, IsHot = false },
                new Product { Name = "HP Pavilion Plus 14 512GB Xanh", Description = "Intel Core i7-13700H, 16GB RAM, 512GB SSD, 14 inch 2.8K OLED.", Price = 29990000, ImageUrl = "/images/products/HP_Pavilion_Plus_14.png", CategoryId = laptopCat.CategoryId, SupplierId = hp.SupplierId, IsNew = true, IsHot = false },
                new Product { Name = "HP Victus 15 512GB Đen", Description = "AMD Ryzen 5 7535HS, RTX 2050, 8GB RAM, 512GB SSD, 15.6 inch 144Hz.", Price = 18990000, ImageUrl = "/images/products/HP_Victus_15.png", CategoryId = laptopCat.CategoryId, SupplierId = hp.SupplierId, IsNew = false, IsHot = true },
                new Product { Name = "Lenovo ThinkPad X1 Carbon 512GB Đen", Description = "Intel Core i7-1365U, 16GB RAM, 512GB SSD, 14 inch 2.8K OLED.", Price = 49990000, ImageUrl = "/images/products/ThinkPad_X1_Carbon.png", CategoryId = laptopCat.CategoryId, SupplierId = lenovo.SupplierId, IsNew = false, IsHot = false },
                new Product { Name = "Lenovo IdeaPad Gaming 3 512GB Đen", Description = "AMD Ryzen 5 5600H, RTX 3050, 8GB RAM, 512GB SSD, 15.6 inch 120Hz.", Price = 16990000, ImageUrl = "/images/products/IdeaPad_Gaming_3.png", CategoryId = laptopCat.CategoryId, SupplierId = lenovo.SupplierId, IsNew = false, IsHot = true },

                // ========== TABLET (5 mẫu) ==========
                new Product { Name = "iPad Pro 12.9 inch M2 256GB Xám", Description = "M2 chip, 12.9-inch Liquid Retina XDR, 256GB, WiFi + 5G.", Price = 32990000, ImageUrl = "/images/products/iPad_Pro_12.9.png", CategoryId = tabletCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true, IsHot = false },
                new Product { Name = "iPad Air M2 256GB Tím", Description = "Chip M2, 11 inch Liquid Retina, 256GB, WiFi, hỗ trợ Apple Pencil Pro.", Price = 22990000, ImageUrl = "/images/products/iPad_Air_M2.png", CategoryId = tabletCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true, IsHot = true },
                new Product { Name = "iPad mini 6 64GB Hồng", Description = "Chip A15 Bionic, 8.3 inch Liquid Retina, hỗ trợ Apple Pencil Gen 2.", Price = 14990000, ImageUrl = "/images/products/iPad_mini_6.png", CategoryId = tabletCat.CategoryId, SupplierId = apple.SupplierId, IsNew = false, IsHot = false },
                new Product { Name = "Samsung Galaxy Tab S9 Ultra 256GB Đen", Description = "Snapdragon 8 Gen 2, 14.6 inch AMOLED 120Hz, S Pen included.", Price = 28990000, ImageUrl = "/images/products/Tab_S9_Ultra.png", CategoryId = tabletCat.CategoryId, SupplierId = samsung.SupplierId, IsNew = true, IsHot = true },
                new Product { Name = "Samsung Galaxy Tab S9 FE 128GB Bạc", Description = "Exynos 1380, 10.9 inch LCD, S Pen included, 128GB.", Price = 9990000, ImageUrl = "/images/products/Tab_S9_FE.png", CategoryId = tabletCat.CategoryId, SupplierId = samsung.SupplierId, IsNew = false, IsHot = false },

                // ========== TAI NGHE & ÂM THANH (5 mẫu) ==========
                new Product { Name = "Sony WH-1000XM5 Đen", Description = "Tai nghe chống ồn cao cấp, 30 giờ pin, LDAC, 8 micro khử ồn AI.", Price = 9990000, ImageUrl = "/images/products/Sony_WH-1000XM5.png", CategoryId = audioCat.CategoryId, SupplierId = sony.SupplierId, IsNew = true, IsHot = true },
                new Product { Name = "AirPods Pro 2 Trắng", Description = "Chip H2, Active Noise Cancellation, Adaptive Audio, USB-C.", Price = 6490000, ImageUrl = "/images/products/AirPods_Pro_2.png", CategoryId = audioCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true, IsHot = true },
                new Product { Name = "Samsung Galaxy Buds2 Pro Trắng", Description = "Tai nghe True Wireless, chống ồn chủ động, 360 Audio, IPX7.", Price = 4990000, ImageUrl = "/images/products/Galaxy_Buds2_Pro.png", CategoryId = audioCat.CategoryId, SupplierId = samsung.SupplierId, IsNew = false, IsHot = false },
                new Product { Name = "JBL Tune 770NC Đen", Description = "Tai nghe over-ear chống ồn, 70 giờ pin, JBL Pure Bass Sound.", Price = 2990000, ImageUrl = "/images/products/JBL_Tune_770NC.png", CategoryId = audioCat.CategoryId, SupplierId = jbl.SupplierId, IsNew = false, IsHot = true },
                new Product { Name = "JBL Flip 6 Xanh", Description = "Loa bluetooth chống nước IPX7, 12 giờ pin, JBL Pro Sound.", Price = 2490000, ImageUrl = "/images/products/JBL_Flip_6.png", CategoryId = audioCat.CategoryId, SupplierId = jbl.SupplierId, IsNew = false, IsHot = false },

                // ========== BÀN PHÍM & CHUỘT (5 mẫu) ==========
                new Product { Name = "Logitech MX Master 3S Đen", Description = "Chuột không dây cao cấp, 8K DPI, kết nối 3 thiết bị.", Price = 2490000, ImageUrl = "/images/products/MX_Master_3S.png", CategoryId = keyboardCat.CategoryId, SupplierId = logitech.SupplierId, IsNew = true, IsHot = true },
                new Product { Name = "Keychron K3 Pro Đen", Description = "Bàn phím cơ low-profile, switch Gateron, kết nối đa thiết bị.", Price = 2290000, ImageUrl = "/images/products/Keychron_K3_Pro.png", CategoryId = keyboardCat.CategoryId, SupplierId = logitech.SupplierId, IsNew = false, IsHot = false },
                new Product { Name = "Logitech G Pro X Superlight 2 Trắng", Description = "Chuột gaming siêu nhẹ 60g, HERO 25K sensor, 95h pin.", Price = 3490000, ImageUrl = "/images/products/G_Pro_X_Superlight_2.png", CategoryId = keyboardCat.CategoryId, SupplierId = logitech.SupplierId, IsNew = true, IsHot = true },
                new Product { Name = "Corsair K70 RGB Pro Đen", Description = "Bàn phím gaming cơ, switch Cherry MX, RGB per-key.", Price = 3990000, ImageUrl = "/images/products/Corsair_K70_RGB_Pro.png", CategoryId = keyboardCat.CategoryId, SupplierId = corsair.SupplierId, IsNew = false, IsHot = false },
                new Product { Name = "Logitech G502 X Plus Trắng", Description = "Chuột gaming, HERO 25K sensor, 13 nút lập trình.", Price = 1990000, ImageUrl = "/images/products/G502_X_Plus.png", CategoryId = keyboardCat.CategoryId, SupplierId = logitech.SupplierId, IsNew = false, IsHot = true },

                // ========== PHỤ KIỆN (5 mẫu) ==========
                new Product { Name = "Anker 735 65W GaN Đen", Description = "Sạc nhanh GaN 65W, 3 cổng USB-C, siêu nhỏ gọn.", Price = 1290000, ImageUrl = "/images/products/Anker_735_65W.png", CategoryId = accessoryCat.CategoryId, SupplierId = anker.SupplierId, IsNew = true, IsHot = true },
                new Product { Name = "Anker PowerCore 20000 Đen", Description = "Pin dự phòng 20000mAh, sạc nhanh PowerIQ.", Price = 1490000, ImageUrl = "/images/products/Anker PowerCore.jpg", CategoryId = accessoryCat.CategoryId, SupplierId = anker.SupplierId, IsNew = false, IsHot = false },
                new Product { Name = "HyperDrive Gen2 Bạc", Description = "Hub USB-C 10 in 1, HDMI 4K, SD/microSD, 100W PD.", Price = 2490000, ImageUrl = "/images/products/HyperDrive_Gen2.png", CategoryId = accessoryCat.CategoryId, SupplierId = anker.SupplierId, IsNew = false, IsHot = true },
                new Product { Name = "Targus Newport Đen", Description = "Túi đựng laptop 15.6 inch, chống sốc, nhiều ngăn.", Price = 990000, ImageUrl = "/images/products/Targus_Newport.png", CategoryId = accessoryCat.CategoryId, SupplierId = anker.SupplierId, IsNew = false, IsHot = false },
                new Product { Name = "Samsung T7 1TB Xám", Description = "Ổ SSD di động USB 3.2 Gen 2, tốc độ 1050MB/s.", Price = 2490000, ImageUrl = "/images/products/Samsung_T7_1TB.png", CategoryId = accessoryCat.CategoryId, SupplierId = samsung.SupplierId, IsNew = false, IsHot = true },

                // ========== ĐỒNG HỒ THÔNG MINH (3 mẫu) ==========
                new Product { Name = "Apple Watch Series 9 45mm Đen", Description = "Chip S9, màn hình Always-On Retina, theo dõi sức khỏe.", Price = 11990000, ImageUrl = "/images/products/Apple_Watch_S9.png", CategoryId = watchCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true, IsHot = true },
                new Product { Name = "Samsung Galaxy Watch 6 Classic 47mm Bạc", Description = "Màn hình Super AMOLED 47mm, bezel xoay, LTE.", Price = 9990000, ImageUrl = "/images/products/Galaxy_Watch_6_Classic.png", CategoryId = watchCat.CategoryId, SupplierId = samsung.SupplierId, IsNew = true, IsHot = false },
                new Product { Name = "Xiaomi Watch S3 Đen", Description = "Màn hình AMOLED 1.43 inch, GPS tích hợp, 21 ngày pin.", Price = 3990000, ImageUrl = "/images/products/Xiaomi_Watch_S3.png", CategoryId = watchCat.CategoryId, SupplierId = xiaomi.SupplierId, IsNew = true, IsHot = false },

                new Product { Name = "MSI Modern 15 H 512GB Đen", Description = "Intel Core i7-13700H, RTX 2050, 16GB RAM, 512GB SSD.", Price = 24990000, ImageUrl = "/images/products/MSI_Modern_15_H.png", CategoryId = laptopCat.CategoryId, SupplierId = dell.SupplierId, IsNew = false, IsHot = false },
                new Product { Name = "ASUS ZenBook 14 OLED 512GB Xanh", Description = "Intel Core Ultra 7, 16GB RAM, 512GB SSD, 14 inch 2.8K OLED.", Price = 27990000, ImageUrl = "/images/products/ZenBook_14_OLED.png", CategoryId = laptopCat.CategoryId, SupplierId = asus.SupplierId, IsNew = true, IsHot = true },
                new Product { Name = "Acer Swift Go 14 512GB Bạc", Description = "Intel Core Ultra 5, 16GB RAM, 512GB SSD, 14 inch 2.8K OLED.", Price = 21990000, ImageUrl = "/images/products/Acer_Swift_Go_14.png", CategoryId = laptopCat.CategoryId, SupplierId = dell.SupplierId, IsNew = true, IsHot = true },
                new Product { Name = "Surface Laptop 5 512GB Bạch Kim", Description = "Intel Core i7-1265U, 16GB RAM, 512GB SSD, 13.5 inch PixelSense.", Price = 39990000, ImageUrl = "/images/products/Surface_Laptop_5.png", CategoryId = laptopCat.CategoryId, SupplierId = dell.SupplierId, IsNew = false, IsHot = false },
                new Product { Name = "Realme C67 128GB Xanh", Description = "Snapdragon 685, camera 108MP, pin 5000mAh, sạc nhanh 33W.", Price = 4990000, ImageUrl = "/images/products/Realme_C67.png", CategoryId = phoneCat.CategoryId, SupplierId = realme.SupplierId, IsNew = false, IsHot = true },
                new Product { Name = "OPPO Reno11 F 5G 256GB Xanh", Description = "Dimensity 7050, camera 64MP OIS, sạc nhanh 67W SUPERVOOC.", Price = 9990000, ImageUrl = "/images/products/Reno11_F_5G.png", CategoryId = phoneCat.CategoryId, SupplierId = oppo.SupplierId, IsNew = true, IsHot = false },
                new Product { Name = "Xiaomi Redmi Note 13 Pro 256GB Đen", Description = "Snapdragon 7s Gen 2, camera 200MP OIS, sạc nhanh 120W.", Price = 7990000, ImageUrl = "/images/products/Redmi_Note_13_Pro.png", CategoryId = phoneCat.CategoryId, SupplierId = xiaomi.SupplierId, IsNew = true, IsHot = true },
                new Product { Name = "Xiaomi POCO X6 Pro 256GB Vàng", Description = "Dimensity 8300-Ultra, camera 64MP OIS, 120Hz AMOLED.", Price = 9990000, ImageUrl = "/images/products/POCO_X6_Pro.png", CategoryId = phoneCat.CategoryId, SupplierId = xiaomi.SupplierId, IsNew = true, IsHot = true },
                new Product { Name = "Xiaomi Pad 6 128GB Xám", Description = "Snapdragon 870, 11 inch 2.8K LCD, 144Hz, hỗ trợ stylus.", Price = 8990000, ImageUrl = "/images/products/Xiaomi_Pad_6.png", CategoryId = tabletCat.CategoryId, SupplierId = xiaomi.SupplierId, IsNew = false, IsHot = true },
                new Product { Name = "Huawei MatePad 11.5 128GB Bạc", Description = "Snapdragon 7 Gen 1, 11.5 inch 2K LCD, 120Hz.", Price = 7990000, ImageUrl = "/images/products/MatePad_11.5.png", CategoryId = tabletCat.CategoryId, SupplierId = xiaomi.SupplierId, IsNew = false, IsHot = false },
                new Product { Name = "Samsung Galaxy Tab S9 FE+ 128GB Xám", Description = "Exynos 1380, 12.4 inch LCD, S Pen included, 128GB.", Price = 15990000, ImageUrl = "/images/products/Tab_S9_FE.png", CategoryId = tabletCat.CategoryId, SupplierId = samsung.SupplierId, IsNew = false, IsHot = true },
                new Product { Name = "OPPO Pad Air2 128GB Tím", Description = "Helio G99, 11.35 inch FHD+, 4 loa Harman Kardon.", Price = 5990000, ImageUrl = "/images/products/OPPO_Pad_Air2.png", CategoryId = tabletCat.CategoryId, SupplierId = oppo.SupplierId, IsNew = false, IsHot = false },
                new Product { Name = "Realme Pad X 128GB Xanh", Description = "Snapdragon 695, 10.95 inch 2K LCD, 5G, pin 8340mAh.", Price = 6990000, ImageUrl = "/images/products/Realme_Pad_X.png", CategoryId = tabletCat.CategoryId, SupplierId = realme.SupplierId, IsNew = false, IsHot = true },
                new Product { Name = "Vivo V30e 5G 256GB Nâu", Description = "Snapdragon 6 Gen 1, camera 50MP Sony IMX882.", Price = 8990000, ImageUrl = "/images/products/V30e_5G.png", CategoryId = phoneCat.CategoryId, SupplierId = oppo.SupplierId, IsNew = true, IsHot = false },
                new Product { Name = "Nokia G22 128GB Xanh", Description = "Unisoc T606, camera 50MP, pin 5050mAh.", Price = 3990000, ImageUrl = "/images/products/Nokia_G22.png", CategoryId = phoneCat.CategoryId, SupplierId = xiaomi.SupplierId, IsNew = false, IsHot = false }
            };


            try
            {
                context.Products.AddRange(products);
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"❌ Lỗi khi lưu Products: {ex.InnerException?.Message ?? ex.Message}");
                throw;
            }

            // ========== PRODUCT VARIANTS (Tạo biến thể động cho tất cả sản phẩm) ==========
            var productList = await context.Products.ToListAsync();

            foreach (var p in productList)
            {
                var variants = new List<ProductVariant>();
                
                // Logic tạo tối thiểu 2 biến thể cho mỗi sản phẩm dựa trên Category
                if (p.CategoryId == phoneCat.CategoryId || p.CategoryId == tabletCat.CategoryId)
                {
                    // Biến thể Dung lượng cho Điện thoại & Máy tính bảng
                    string name = p.Name;
                    string currentStorage = "128GB";
                    if (name.Contains("256GB")) currentStorage = "256GB";
                    else if (name.Contains("512GB")) currentStorage = "512GB";
                    else if (name.Contains("1TB")) currentStorage = "1TB";
                    else if (name.Contains("64GB")) currentStorage = "64GB";

                    string nextStorage = currentStorage switch
                    {
                        "64GB" => "128GB",
                        "128GB" => "256GB",
                        "256GB" => "512GB",
                        "512GB" => "1TB",
                        _ => "2TB"
                    };

                    variants.Add(new ProductVariant { ProductId = p.ProductId, Storage = currentStorage, Color = "Mặc định", Price = p.Price, StockQuantity = 50 });
                    variants.Add(new ProductVariant { ProductId = p.ProductId, Storage = nextStorage, Color = "Mặc định", Price = p.Price + 3000000, StockQuantity = 30 });
                }
                else if (p.CategoryId == laptopCat.CategoryId)
                {
                    // Biến thể RAM & SSD cho Laptop
                    variants.Add(new ProductVariant { ProductId = p.ProductId, RAM = "16GB", Storage = "512GB", Price = p.Price, StockQuantity = 20 });
                    variants.Add(new ProductVariant { ProductId = p.ProductId, RAM = "32GB", Storage = "1TB", Price = p.Price + 5000000, StockQuantity = 10 });
                }
                else if (p.CategoryId == audioCat.CategoryId || p.CategoryId == watchCat.CategoryId)
                {
                    // Biến thể Màu sắc cho Tai nghe & Đồng hồ
                    variants.Add(new ProductVariant { ProductId = p.ProductId, Color = "Đen (Space Black)", Price = p.Price, StockQuantity = 40 });
                    variants.Add(new ProductVariant { ProductId = p.ProductId, Color = "Trắng (Pearl White)", Price = p.Price, StockQuantity = 35 });
                }
                else
                {
                    // Biến thể mặc định cho các loại khác
                    variants.Add(new ProductVariant { ProductId = p.ProductId, Color = "Bản tiêu chuẩn", Price = p.Price, StockQuantity = 100 });
                    variants.Add(new ProductVariant { ProductId = p.ProductId, Color = "Bản cao cấp", Price = p.Price + 1000000, StockQuantity = 60 });
                }

                context.ProductVariants.AddRange(variants);
            }

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"❌ Lỗi khi lưu ProductVariants: {ex.InnerException?.Message ?? ex.Message}");
                throw;
            }


            // ========== INVENTORY (50 mẫu - 1 cho mỗi sản phẩm) ==========
            productList = await context.Products.ToListAsync();
            var inventories = productList.Select((p, index) => new Inventory
            {
                ProductId = p.ProductId,
                StockQuantity = new Random(index).Next(10, 200),
                LastUpdated = DateTime.Now
            }).ToList();
            context.Inventory.AddRange(inventories);
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"❌ Lỗi khi lưu Inventory: {ex.InnerException?.Message ?? ex.Message}");
                throw;
            }

            // ========== FAQ (20 mẫu) ==========
            var faqs = new List<FAQ>
            {
                new FAQ { Question = "Làm sao để đặt hàng?", Answer = "Bạn có thể đặt hàng trực tiếp trên website bằng cách chọn sản phẩm, thêm vào giỏ hàng và tiến hành thanh toán.", Category = "Đặt hàng", Priority = 1 },
                new FAQ { Question = "Các phương thức thanh toán nào được chấp nhận?", Answer = "Chúng tôi chấp nhận thanh toán qua: COD (nhận hàng rồi trả tiền), chuyển khoản ngân hàng, và thanh toán qua VNPAY.", Category = "Thanh toán", Priority = 1 },
                new FAQ { Question = "Thời gian giao hàng là bao lâu?", Answer = "Đơn hàng nội thành TP.HCM: 1-2 ngày. Các tỉnh thành khác: 2-5 ngày tùy khu vực.", Category = "Vận chuyển", Priority = 1 },
                new FAQ { Question = "Chính sách đổi trả như thế nào?", Answer = "Quý khách được đổi trả trong vòng 7 ngày nếu sản phẩm bị lỗi từ nhà sản xuất hoặc không đúng mô tả.", Category = "Đổi trả", Priority = 1 },
                new FAQ { Question = "Làm sao để liên hệ hỗ trợ?", Answer = "Bạn có thể liên hệ qua hotline 0123-456-789, email support@webstore.com hoặc chat trực tiếp trên website.", Category = "Hỗ trợ", Priority = 1 },
                new FAQ { Question = "Có giao hàng COD không?", Answer = "Có, chúng tôi hỗ trợ thanh toán khi nhận hàng (COD) cho tất cả các đơn hàng trên toàn quốc.", Category = "Thanh toán", Priority = 2 },
                new FAQ { Question = "Làm sao để theo dõi đơn hàng?", Answer = "Sau khi đặt hàng thành công, bạn sẽ nhận được mã đơn hàng. Truy cập trang 'Lịch sử đơn hàng' để theo dõi.", Category = "Đặt hàng", Priority = 2 },
                new FAQ { Question = "Sản phẩm có bảo hành không?", Answer = "Tất cả sản phẩm đều được bảo hành chính hãng. Thời gian bảo hành tùy theo từng sản phẩm (12-24 tháng).", Category = "Bảo hành", Priority = 1 },
                new FAQ { Question = "Tôi có thể hủy đơn hàng không?", Answer = "Bạn có thể hủy đơn hàng trước khi sản phẩm được giao. Liên hệ hotline hoặc chat với chúng tôi để được hỗ trợ.", Category = "Đặt hàng", Priority = 2 },
                new FAQ { Question = "Phí vận chuyển là bao nhiêu?", Answer = "Miễn phí vận chuyển cho đơn hàng từ 500.000đ trở lên. Đơn hàng dưới 500.000đ phí ship 30.000đ.", Category = "Vận chuyển", Priority = 1 },
                new FAQ { Question = "Sản phẩm có chính hãng không?", Answer = "Tất cả sản phẩm tại Webstore đều là hàng chính hãng 100%, có đầy đủ hóa đơn và bảo hành từ nhà sản xuất.", Category = "Sản phẩm", Priority = 1 },
                new FAQ { Question = "Làm sao để đăng ký tài khoản?", Answer = "Click vào nút 'Đăng ký' ở góc phải màn hình, điền thông tin cá nhân và tạo mật khẩu để đăng ký.", Category = "Tài khoản", Priority = 2 },
                new FAQ { Question = "Quên mật khẩu thì làm sao?", Answer = "Click vào 'Quên mật khẩu' ở trang đăng nhập, nhập email đã đăng ký và làm theo hướng dẫn trong email.", Category = "Tài khoản", Priority = 2 },
                new FAQ { Question = "Tôi có thể cập nhật thông tin cá nhân không?", Answer = "Có, đăng nhập vào tài khoản, vào mục 'Tài khoản' để cập nhật thông tin cá nhân của bạn.", Category = "Tài khoản", Priority = 3 },
                new FAQ { Question = "Có chương trình khuyến mãi không?", Answer = "Chúng tôi thường xuyên có các chương trình khuyến mãi, flash sale và mã giảm giá. Theo dõi trang chủ để cập nhật.", Category = "Khuyến mãi", Priority = 2 },
                new FAQ { Question = "Sản phẩm có đầy đủ phụ kiện không?", Answer = "Tất cả sản phẩm đều có đầy đủ phụ kiện theo quy định của nhà sản xuất. Phụ kiện tặng kèm sẽ được ghi chú rõ trong mô tả.", Category = "Sản phẩm", Priority = 3 },
                new FAQ { Question = "Thanh toán qua VNPAY an toàn không?", Answer = "Rất an toàn! VNPAY là cổng thanh toán uy tín, được cấp phép bởi Ngân hàng Nhà nước Việt Nam.", Category = "Thanh toán", Priority = 2 },
                new FAQ { Question = "Tôi có thể mua sỉ không?", Answer = "Có, chúng tôi có chính sách bán sỉ cho các đơn hàng lớn. Liên hệ hotline để được báo giá sỉ.", Category = "Mua sỉ", Priority = 3 },
                new FAQ { Question = "Sản phẩm còn hàng không?", Answer = "Website cập nhật số lượng tồn kho theo thời gian thực. Nếu sản phẩm hết hàng, bạn có thể đặt trước.", Category = "Sản phẩm", Priority = 2 },
                new FAQ { Question = "Làm sao để viết đánh giá sản phẩm?", Answer = "Sau khi nhận được hàng và đăng nhập, vào trang chi tiết sản phẩm để viết đánh giá và đánh sao.", Category = "Đánh giá", Priority = 3 }
            };
            try
            {
                context.FAQs.AddRange(faqs);
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"❌ Lỗi khi lưu FAQs: {ex.InnerException?.Message ?? ex.Message}");
                throw;
            }
            // ========== ĐƠN HÀNG MẪU (20 đơn, trải đều 7 ngày) ==========
            var today = DateTime.Today;
            var rng = new Random(42);
            var allProducts = await context.Products.ToListAsync();
            var customerAccounts = await context.Accounts.Where(a => a.Role == "Customer").ToListAsync();

            var statuses = new[] { "New", "Processing", "Delivered", "Delivered", "Delivered", "Delivered", "Delivered", "Processing", "Canceled", "New" };

            // Tạo trước danh sách order items cho từng đơn
            var orderDataList = new List<(Order order, List<OrderItem> items)>();

            for (int i = 0; i < 20; i++)
            {
                var daysAgo = rng.Next(0, 7);
                var customer = customerAccounts[rng.Next(customerAccounts.Count)];
                var status = statuses[i % statuses.Length];

                // Chỉ tạo 1-2 sản phẩm mỗi đơn và số lượng 1 để tổng tiền không vượt quá decimal(10,2)
                var itemCount = rng.Next(1, 3);
                decimal total = 0;
                var items = new List<OrderItem>();
                var usedProducts = new HashSet<int>();

                for (int j = 0; j < itemCount; j++)
                {
                    Product product;
                    do { product = allProducts[rng.Next(allProducts.Count)]; }
                    while (usedProducts.Contains(product.ProductId));
                    usedProducts.Add(product.ProductId);

                    var qty = 1; // Giữ qty = 1 để tránh lỗi Arithmetic overflow (decimal 10,2)
                    var unitPrice = product.Price;
                    total += qty * unitPrice;

                    items.Add(new OrderItem
                    {
                        ProductId = product.ProductId,
                        Quantity = qty,
                        UnitPrice = unitPrice
                    });
                }
                
                // Nếu vượt quá 99 triệu (giới hạn của decimal(10,2)), gán cứng thành 99 triệu
                if (total > 99000000m)
                {
                    total = 99000000m;
                }

                var order = new Order
                {
                    AccountId = customer.AccountId,
                    OrderDate = today.AddDays(-daysAgo).AddHours(rng.Next(8, 22)).AddMinutes(rng.Next(0, 60)),
                    TotalAmount = total,
                    Status = status,
                    CustomerName = customer.FullName,
                    CustomerPhone = customer.Phone,
                    CustomerAddress = customer.Address,
                    Notes = i % 3 == 0 ? "Giao giờ hành chính" : null
                };

                orderDataList.Add((order, items));
            }

            // Lưu từng đơn hàng một để tránh conflict composite key (OrderId, ProductId)
            foreach (var (order, items) in orderDataList)
            {
                context.Orders.Add(order);
                await context.SaveChangesAsync(); // lấy OrderId trước

                foreach (var item in items)
                {
                    item.OrderId = order.OrderId;
                    context.OrderItems.Add(item);
                }
                await context.SaveChangesAsync();
            }
        }

    }
}

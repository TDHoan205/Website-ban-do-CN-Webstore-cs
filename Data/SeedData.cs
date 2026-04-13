using Microsoft.EntityFrameworkCore;
using Webstore.Models;
using Webstore.Models.Security;

namespace Webstore.Data
{
    public static class SeedData
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Kiểm tra xem đã có dữ liệu chưa
            if (await context.Accounts.AnyAsync())
            {
                return; // Đã có dữ liệu, không cần seed
            }

            // Tạo tài khoản Admin
            var adminAccount = new Account
            {
                Username = "admin",
                FullName = "Quản trị viên",
                Email = "admin@webstore.com",
                Phone = "0123456789",
                Address = "123 Đường ABC, Quận 1, TP.HCM",
                Role = "Admin",
                PasswordHash = "admin123" // Mật khẩu plain text
            };

            // Tạo tài khoản Employee
            var employeeAccount = new Account
            {
                Username = "employee",
                FullName = "Nhân viên",
                Email = "employee@webstore.com",
                Phone = "0987654321",
                Address = "456 Đường XYZ, Quận 2, TP.HCM",
                Role = "Employee",
                PasswordHash = "employee123" // Mật khẩu plain text
            };

            // Tạo tài khoản Customer
            var customerAccount = new Account
            {
                Username = "customer",
                FullName = "Khách hàng",
                Email = "customer@webstore.com",
                Phone = "0555666777",
                Address = "789 Đường DEF, Quận 3, TP.HCM",
                Role = "Customer",
                PasswordHash = "customer123" // Mật khẩu plain text
            };

            context.Accounts.AddRange(adminAccount, employeeAccount, customerAccount);

            // Tạo danh mục thiết bị công nghệ
            var categories = new List<Category>
            {
                new Category { Name = "Điện thoại di động" },
                new Category { Name = "Laptop & Máy tính" },
                new Category { Name = "Tablet" },
                new Category { Name = "Phụ kiện điện tử" },
                new Category { Name = "Thiết bị mạng" },
                new Category { Name = "Thiết bị âm thanh" },
                new Category { Name = "Máy ảnh & Quay phim" },
                new Category { Name = "Linh kiện máy tính" }
            };
            context.Categories.AddRange(categories);

            // Tạo nhà cung cấp công nghệ
            var suppliers = new List<Supplier>
            {
                new Supplier {
                    Name = "Apple Vietnam",
                    Email = "contact@apple.com.vn",
                    Phone = "1800-1192",
                    ContactPerson = "Phòng Kinh Doanh",
                    Address = "Lầu 3, Sheraton Plaza, 175 Đồng Khởi, Q.1, TP.HCM"
                },
                new Supplier {
                    Name = "Samsung Electronics Vietnam",
                    Email = "contact@samsung.com.vn",
                    Phone = "1800-588-889",
                    ContactPerson = "Bộ phận Đối tác Bán lẻ",
                    Address = "Tòa nhà PVI, 1 Phạm Văn Bạch, Cầu Giấy, Hà Nội"
                },
                new Supplier {
                    Name = "Dell Vietnam",
                    Email = "dell@vietnam.com",
                    Phone = "1800-545-455",
                    ContactPerson = "Phòng Phân phối",
                    Address = "Tòa nhà Empress, 128-128 Bis Hồng Bàng, Q.5, TP.HCM"
                },
                new Supplier {
                    Name = "Sony Vietnam",
                    Email = "sony@vietnam.com",
                    Phone = "1800-588-880",
                    ContactPerson = "Phòng Kinh doanh",
                    Address = "Tòa nhà Vietnam Business Center, Q.1, TP.HCM"
                },
                new Supplier {
                    Name = "LG Electronics Vietnam",
                    Email = "lg@vietnam.com",
                    Phone = "1800-1503",
                    ContactPerson = "Bộ phận Đối tác",
                    Address = "Tòa nhà Keangnam Landmark, Phạm Hùng, Hà Nội"
                }
            };
            context.Suppliers.AddRange(suppliers);

            await context.SaveChangesAsync();

            // Lấy ID của categories và suppliers vừa tạo (dùng FirstOrDefault + fallback null-safe)
            var phoneCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Điện thoại di động");
            var laptopCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Laptop & Máy tính");
            var tabletCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Tablet");
            var accessoryCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Phụ kiện điện tử");
            var audioCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Thiết bị âm thanh");

            var appleSupplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Name == "Apple Vietnam");
            var samsungSupplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Name == "Samsung Electronics Vietnam");
            var dellSupplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Name == "Dell Vietnam");
            var sonySupplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Name == "Sony Vietnam");
            var lgSupplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Name == "LG Electronics Vietnam");

            // Fallback placeholder cho ảnh (dùng ảnh cục bộ tránh broken external links)
            var placeholderImg = "/images/products/placeholder.png";

            // Tạo sản phẩm công nghệ mẫu
            var products = new List<Product>();

            if (phoneCategory != null && appleSupplier != null)
            {
                products.Add(new Product
                {
                    Name = "iPhone 15 Pro Max 256GB",
                    Description = "iPhone 15 Pro Max. Titan. Nút Action. Chip A17 Pro. Camera chính 48MP. Dynamic Island. Pin siêu bền cả ngày.",
                    Price = 34990000,
                    ImageUrl = placeholderImg,
                    CategoryId = phoneCategory.CategoryId,
                    SupplierId = appleSupplier.SupplierId
                });
            }

            if (phoneCategory != null && samsungSupplier != null)
            {
                products.Add(new Product
                {
                    Name = "Samsung Galaxy S24 Ultra 5G",
                    Description = "Galaxy S24 Ultra với S Pen tích hợp, màn hình Dynamic AMOLED 2X 6.8 inch, camera 200MP, pin 5000mAh, hỗ trợ AI.",
                    Price = 29990000,
                    ImageUrl = placeholderImg,
                    CategoryId = phoneCategory.CategoryId,
                    SupplierId = samsungSupplier.SupplierId
                });
            }

            if (laptopCategory != null && appleSupplier != null)
            {
                products.Add(new Product
                {
                    Name = "MacBook Pro 14 inch M3 Pro",
                    Description = "Chip M3 Pro 12-core CPU, 18-core GPU, 18GB RAM, 512GB SSD, Liquid Retina XDR display, pin 17h.",
                    Price = 49990000,
                    ImageUrl = placeholderImg,
                    CategoryId = laptopCategory.CategoryId,
                    SupplierId = appleSupplier.SupplierId
                });
            }

            if (laptopCategory != null && dellSupplier != null)
            {
                products.Add(new Product
                {
                    Name = "Dell XPS 15 9530",
                    Description = "Intel Core i9-13900H, RTX 4070, 32GB RAM, 1TB SSD, 15.6 inch 3.5K OLED Touch, Windows 11.",
                    Price = 69990000,
                    ImageUrl = placeholderImg,
                    CategoryId = laptopCategory.CategoryId,
                    SupplierId = dellSupplier.SupplierId
                });
            }

            if (audioCategory != null && sonySupplier != null)
            {
                products.Add(new Product
                {
                    Name = "Sony WH-1000XM5",
                    Description = "Tai nghe chống ồn cao cấp nhất của Sony, 30 giờ pin, LDAC, kết nối Multipoint, 8 micro khử ồn AI.",
                    Price = 9990000,
                    ImageUrl = placeholderImg,
                    CategoryId = audioCategory.CategoryId,
                    SupplierId = sonySupplier.SupplierId
                });
            }

            if (tabletCategory != null && appleSupplier != null)
            {
                products.Add(new Product
                {
                    Name = "iPad Pro 12.9 inch M2",
                    Description = "M2 chip mạnh mẽ, 12.9-inch Liquid Retina XDR display, 256GB, WiFi + 5G, hỗ trợ Apple Pencil Gen 2.",
                    Price = 32990000,
                    ImageUrl = placeholderImg,
                    CategoryId = tabletCategory.CategoryId,
                    SupplierId = appleSupplier.SupplierId
                });
            }

            if (accessoryCategory != null && lgSupplier != null)
            {
                products.Add(new Product
                {
                    Name = "LG UltraGear 27GR95QE-B",
                    Description = "Màn hình gaming 27 inch OLED, 2K QHD, 240Hz, 0.03ms GtG, G-Sync Compatible, FreeSync Premium Pro.",
                    Price = 19990000,
                    ImageUrl = placeholderImg,
                    CategoryId = accessoryCategory.CategoryId,
                    SupplierId = lgSupplier.SupplierId
                });
            }

            // Thêm 13 sản phẩm mẫu còn lại để đạt 20 sản phẩm
            if (phoneCategory != null)
            {
                products.Add(new Product { Name = "iPhone 15 Pro 128GB", Description = "Chip A17 Pro, camera 48MP, titanium grade 5, USB-C 3.0.", Price = 27990000, ImageUrl = placeholderImg, CategoryId = phoneCategory.CategoryId, SupplierId = appleSupplier?.SupplierId ?? 0 });
                products.Add(new Product { Name = "iPhone 15 256GB", Description = "Chip A16 Bionic, camera 48MP, Dynamic Island, pin 24h.", Price = 22990000, ImageUrl = placeholderImg, CategoryId = phoneCategory.CategoryId, SupplierId = appleSupplier?.SupplierId ?? 0 });
                products.Add(new Product { Name = "Samsung Galaxy Z Fold5", Description = "Màn hình gập 7.6 inch, Snapdragon 8 Gen 2, camera 50MP.", Price = 39990000, ImageUrl = placeholderImg, CategoryId = phoneCategory.CategoryId, SupplierId = samsungSupplier?.SupplierId ?? 0 });
                products.Add(new Product { Name = "Samsung Galaxy Z Flip5", Description = "Màn hình gập nhỏ gọn 6.7 inch, Snapdragon 8 Gen 2, Flex Mode.", Price = 22990000, ImageUrl = placeholderImg, CategoryId = phoneCategory.CategoryId, SupplierId = samsungSupplier?.SupplierId ?? 0 });
                products.Add(new Product { Name = "Xiaomi 14 Pro", Description = "Snapdragon 8 Gen 3, Leica camera 50MP, 120W HyperCharge.", Price = 19990000, ImageUrl = placeholderImg, CategoryId = phoneCategory.CategoryId, SupplierId = samsungSupplier?.SupplierId ?? 0 });
            }

            if (laptopCategory != null)
            {
                products.Add(new Product { Name = "MacBook Air 15 M3", Description = "Chip M3, 15.3 inch Liquid Retina, 18GB RAM, 256GB SSD, pin 18h.", Price = 34990000, ImageUrl = placeholderImg, CategoryId = laptopCategory.CategoryId, SupplierId = appleSupplier?.SupplierId ?? 0 });
                products.Add(new Product { Name = "Dell Inspiron 15 3530", Description = "Intel Core i5-1335U, 8GB RAM, 512GB SSD, 15.6 inch FHD.", Price = 15990000, ImageUrl = placeholderImg, CategoryId = laptopCategory.CategoryId, SupplierId = dellSupplier?.SupplierId ?? 0 });
                products.Add(new Product { Name = "ASUS ROG Zephyrus G14", Description = "AMD Ryzen 9 7940HS, RTX 4070, 16GB RAM, 1TB SSD, 14 inch.", Price = 54990000, ImageUrl = placeholderImg, CategoryId = laptopCategory.CategoryId, SupplierId = dellSupplier?.SupplierId ?? 0 });
                products.Add(new Product { Name = "HP Pavilion 15", Description = "Intel Core i7-1355U, 16GB RAM, 512GB SSD, 15.6 inch IPS.", Price = 21990000, ImageUrl = placeholderImg, CategoryId = laptopCategory.CategoryId, SupplierId = dellSupplier?.SupplierId ?? 0 });
                products.Add(new Product { Name = "Lenovo ThinkPad X1 Carbon", Description = "Intel Core i7-1365U, 16GB RAM, 512GB SSD, 14 inch 2.8K OLED.", Price = 49990000, ImageUrl = placeholderImg, CategoryId = laptopCategory.CategoryId, SupplierId = dellSupplier?.SupplierId ?? 0 });
            }

            if (tabletCategory != null)
            {
                products.Add(new Product { Name = "Samsung Galaxy Tab S9 Ultra", Description = "Snapdragon 8 Gen 2, 14.6 inch AMOLED 120Hz, S Pen included.", Price = 28990000, ImageUrl = placeholderImg, CategoryId = tabletCategory.CategoryId, SupplierId = samsungSupplier?.SupplierId ?? 0 });
            }

            if (audioCategory != null)
            {
                products.Add(new Product { Name = "AirPods Pro 2", Description = "Chip H2, Active Noise Cancellation, Adaptive Audio, USB-C, 6h pin.", Price = 6490000, ImageUrl = placeholderImg, CategoryId = audioCategory.CategoryId, SupplierId = appleSupplier?.SupplierId ?? 0 });
                products.Add(new Product { Name = "Sony WF-1000XM5", Description = "Tai nghe True Wireless chống ồn, 8h pin, LDAC, Multipoint.", Price = 7490000, ImageUrl = placeholderImg, CategoryId = audioCategory.CategoryId, SupplierId = sonySupplier?.SupplierId ?? 0 });
            }

            if (accessoryCategory != null)
            {
                products.Add(new Product { Name = "Logitech MX Master 3S", Description = "Chuột không dây cao cấp, 8K DPI, scroll电磁, kết nối 3 thiết bị.", Price = 2490000, ImageUrl = placeholderImg, CategoryId = accessoryCategory.CategoryId, SupplierId = dellSupplier?.SupplierId ?? 0 });
            }

            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            // Tạo inventory cho các sản phẩm
            var productList = await context.Products.ToListAsync();
            var inventories = new List<Inventory>();

            foreach (var product in productList)
            {
                inventories.Add(new Inventory
                {
                    ProductId = product.ProductId,
                    QuantityInStock = 100,
                    LastUpdatedDate = DateTime.Now
                });
            }
            context.Inventory.AddRange(inventories);

            await context.SaveChangesAsync();
        }
    }
}

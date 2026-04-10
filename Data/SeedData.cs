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

            // Lấy ID của categories và suppliers vừa tạo
            var phoneCategory = await context.Categories.FirstAsync(c => c.Name == "Điện thoại");
            var laptopCategory = await context.Categories.FirstAsync(c => c.Name == "Laptop");
            var appleSupplier = await context.Suppliers.FirstAsync(s => s.Name == "Apple Store");
            var samsungSupplier = await context.Suppliers.FirstAsync(s => s.Name == "Samsung Vietnam");

            // Tạo sản phẩm công nghệ mẫu
            var products = new List<Product>
            {
                new Product
                {
                    Name = "iPhone 15 Pro Max 256GB",
                    Description = "iPhone 15 Pro Max. Titan. Nút Action. Chip A17 Pro. Camera chính 48MP. Dynamic Island.",
                    Price = 34990000,
                    ImageUrl = "https://images.apple.com/media/us/iphone-15/2023/16c928ee-07a5-4a38-8d56-30ba4c6b445c/PDP_LOGO_14__CCCOSD932E2E_FMT_WHH.jpg",
                    CategoryId = phoneCategory.CategoryId,
                    SupplierId = appleSupplier.SupplierId
                },
                new Product
                {
                    Name = "Samsung Galaxy S24 Ultra 5G",
                    Description = "Galaxy S24 Ultra với S Pen, màn hình Dynamic AMOLED 2X 6.8\", camera 200MP, pin 5000mAh",
                    Price = 29990000,
                    ImageUrl = "https://images.samsung.com/us/smartphones/galaxy-s24/galaxy-s24-ultra/assets/images/gallery/galaxy-s24-ultra-titanium-black-gallery-front.jpg",
                    CategoryId = phoneCategory.CategoryId,
                    SupplierId = samsungSupplier.SupplierId
                },
                new Product
                {
                    Name = "MacBook Pro 14\" M3 Pro",
                    Description = "Chip M3 Pro 12-core CPU, 18-core GPU, 18GB RAM, 512GB SSD, Liquid Retina XDR display",
                    Price = 49990000,
                    ImageUrl = "https://images.apple.com/media/us/macbook-pro/2023/16ada7ff-5a81-4491-a67f-fa1aad0a0657/og__cvrebk237zkm_og.jpg",
                    CategoryId = laptopCategory.CategoryId,
                    SupplierId = appleSupplier.SupplierId
                },
                new Product
                {
                    Name = "Dell XPS 15 9530",
                    Description = "Intel Core i9-13900H, RTX 4070, 32GB RAM, 1TB SSD, 15.6\" 3.5K OLED Touch",
                    Price = 69990000,
                    ImageUrl = "https://i.dell.com/is/image/DellContent/contexts/ctx_7521900bc10ec0f7~ctx_92cd643cc5f10bf5/images/transparent/15-9530-FHD-InfinityEdge-Webcam-Hero-500x500-GLF.pngw=500",
                    CategoryId = laptopCategory.CategoryId,
                    SupplierId = context.Suppliers.First(s => s.Name == "Dell Vietnam").SupplierId
                },
                new Product
                {
                    Name = "Sony WH-1000XM5",
                    Description = "Tai nghe chống ồn cao cấp, 30 giờ pin, LDAC, Multipoint, 8 mic",
                    Price = 9990000,
                    ImageUrl = "https://www.sony.com/image/cc/images/en_US/electronics/product_images/WH1000XM5_L.jpg",
                    CategoryId = context.Categories.First(c => c.Name == "Thiết bị âm thanh").CategoryId,
                    SupplierId = context.Suppliers.First(s => s.Name == "Sony Vietnam").SupplierId
                },
                new Product
                {
                    Name = "iPad Pro 12.9\" M2",
                    Description = "M2 chip, 12.9-inch Liquid Retina XDR display, 256GB, WiFi + 5G",
                    Price = 32990000,
                    ImageUrl = "https://images.apple.com/media/us/ipad-pro/2022/296bea55-91e0-4112-9a27-fa34b47cca63/og__bnnpzy8kceea_og.jpg",
                    CategoryId = context.Categories.First(c => c.Name == "Tablet").CategoryId,
                    SupplierId = appleSupplier.SupplierId
                },
                new Product
                {
                    Name = "LG UltraGear 27GR95QE-B",
                    Description = "Màn hình gaming 27\" OLED, 2K, 240Hz, 0.03ms GtG, G-Sync, FreeSync Premium",
                    Price = 19990000,
                    ImageUrl = "https://www.lg.com/us/en/products/gaming/monitors/oled-monitor-27gr95qe-b.jpg",
                    CategoryId = context.Categories.First(c => c.Name == "Phụ kiện điện tử").CategoryId,
                    SupplierId = context.Suppliers.First(s => s.Name == "LG Electronics Vietnam").SupplierId
                }
            };
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

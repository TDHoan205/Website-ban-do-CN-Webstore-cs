using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Webstore.Models;
using Webstore.Models.Security;
using Webstore.Utilities;

namespace Webstore.Data
{
    public static class SeedData
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            await RepairCorruptedTextAsync(context);

            if (await context.Accounts.AnyAsync())
            {
                return;
            }

            // Hash passwords properly: salt:hash format
            string HashPass(string p)
            {
                var salt = PasswordHasher.GenerateSalt();
                return salt + ":" + PasswordHasher.HashPassword(p, salt);
            }

            // Tạo tài khoản mẫu với hashed passwords
            var adminAccount = new Account
            {
                Username = "admin",
                FullName = "Quản trị viên",
                Email = "admin@webstore.com",
                Phone = "0123456789",
                Address = "123 Đường ABC, Quận 1, TP.HCM",
                Role = "Admin",
                PasswordHash = HashPass("admin123"),
                IsActive = true
            };

            var employeeAccount = new Account
            {
                Username = "employee",
                FullName = "Nhân viên kinh doanh",
                Email = "employee@webstore.com",
                Phone = "0987654321",
                Address = "456 Đường XYZ, Quận 2, TP.HCM",
                Role = "Employee",
                PasswordHash = HashPass("employee123"),
                IsActive = true
            };

            var customerAccount = new Account
            {
                Username = "customer",
                FullName = "Khách hàng mẫu",
                Email = "customer@webstore.com",
                Phone = "0555666777",
                Address = "789 Đường DEF, Quận 3, TP.HCM",
                Role = "Customer",
                PasswordHash = HashPass("customer123"),
                IsActive = true
            };

            context.Accounts.AddRange(adminAccount, employeeAccount, customerAccount);

            // Tạo danh mục
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

            // Tạo nhà cung cấp
            var suppliers = new List<Supplier>
            {
                new Supplier { Name = "Apple Vietnam", Email = "contact@apple.com.vn", Phone = "1800-1192", ContactPerson = "Phòng Kinh Doanh", Address = "Lầu 3, Sheraton Plaza, 175 Đồng Khởi, Q.1, TP.HCM" },
                new Supplier { Name = "Samsung Electronics Vietnam", Email = "contact@samsung.com.vn", Phone = "1800-588-889", ContactPerson = "Bộ phận Đối tác Bán lẻ", Address = "Tòa nhà PVI, 1 Phạm Văn Bạch, Cầu Giấy, Hà Nội" },
                new Supplier { Name = "Dell Vietnam", Email = "dell@vietnam.com", Phone = "1800-545-455", ContactPerson = "Phòng Phân phối", Address = "Tòa nhà Empress, 128-128 Bis Hồng Bàng, Q.5, TP.HCM" },
                new Supplier { Name = "Sony Vietnam", Email = "sony@vietnam.com", Phone = "1800-588-880", ContactPerson = "Phòng Kinh doanh", Address = "Tòa nhà Vietnam Business Center, Q.1, TP.HCM" },
                new Supplier { Name = "LG Electronics Vietnam", Email = "lg@vietnam.com", Phone = "1800-1503", ContactPerson = "Bộ phận Đối tác", Address = "Tòa nhà Keangnam Landmark, Phạm Hùng, Hà Nội" }
            };
            context.Suppliers.AddRange(suppliers);

            await context.SaveChangesAsync();

            // Lấy references
            var phoneCat = await context.Categories.FirstAsync(c => c.Name == "Điện thoại di động");
            var laptopCat = await context.Categories.FirstAsync(c => c.Name == "Laptop & Máy tính");
            var tabletCat = await context.Categories.FirstAsync(c => c.Name == "Tablet");
            var accessoryCat = await context.Categories.FirstAsync(c => c.Name == "Phụ kiện điện tử");
            var audioCat = await context.Categories.FirstAsync(c => c.Name == "Thiết bị âm thanh");
            var networkCat = await context.Categories.FirstAsync(c => c.Name == "Thiết bị mạng");
            var cameraCat = await context.Categories.FirstAsync(c => c.Name == "Máy ảnh & Quay phim");
            var componentCat = await context.Categories.FirstAsync(c => c.Name == "Linh kiện máy tính");

            var apple = await context.Suppliers.FirstAsync(s => s.Name == "Apple Vietnam");
            var samsung = await context.Suppliers.FirstAsync(s => s.Name == "Samsung Electronics Vietnam");
            var dell = await context.Suppliers.FirstAsync(s => s.Name == "Dell Vietnam");
            var sony = await context.Suppliers.FirstAsync(s => s.Name == "Sony Vietnam");
            var lg = await context.Suppliers.FirstAsync(s => s.Name == "LG Electronics Vietnam");

            var placeholder = "/images/products/placeholder.png";

            // ========== SẢN PHẨM MẪU ==========
            // Format: new Product { Name, Description, Price, Image, CategoryId, SupplierId, IsNew, IsHot, IsDeal }
            var products = new List<Product>
            {
                // --- ĐIỆN THOẠI ---
                new Product { Name = "iPhone 15 Pro Max 256GB", Description = "Titan grade 5, A17 Pro chip, camera 48MP, Dynamic Island, pin siêu bền cả ngày.", Price = 34990000, ImageUrl = placeholder, CategoryId = phoneCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true, IsHot = true, IsDeal = false },
                new Product { Name = "Samsung Galaxy S24 Ultra 5G", Description = "S Pen tích hợp, màn hình Dynamic AMOLED 2X 6.8 inch, camera 200MP, pin 5000mAh.", Price = 29990000, ImageUrl = placeholder, CategoryId = phoneCat.CategoryId, SupplierId = samsung.SupplierId, IsNew = true, IsHot = true, IsDeal = true },
                new Product { Name = "iPhone 15 Pro 128GB", Description = "Chip A17 Pro, camera 48MP, titanium grade 5, USB-C 3.0.", Price = 27990000, ImageUrl = placeholder, CategoryId = phoneCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true, IsHot = false, IsDeal = false },
                new Product { Name = "iPhone 15 256GB", Description = "Chip A16 Bionic, camera 48MP, Dynamic Island, pin 24h.", Price = 22990000, ImageUrl = placeholder, CategoryId = phoneCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true, IsHot = false, IsDeal = true },
                new Product { Name = "Samsung Galaxy Z Fold5", Description = "Màn hình gập 7.6 inch, Snapdragon 8 Gen 2, camera 50MP, hỗ trợ S Pen.", Price = 39990000, ImageUrl = placeholder, CategoryId = phoneCat.CategoryId, SupplierId = samsung.SupplierId, IsNew = false, IsHot = true, IsDeal = false },
                new Product { Name = "Samsung Galaxy Z Flip5", Description = "Màn hình gập nhỏ gọn 6.7 inch, Snapdragon 8 Gen 2, Flex Mode đa năng.", Price = 22990000, ImageUrl = placeholder, CategoryId = phoneCat.CategoryId, SupplierId = samsung.SupplierId, IsNew = false, IsHot = true, IsDeal = true },
                new Product { Name = "Xiaomi 14 Pro", Description = "Snapdragon 8 Gen 3, Leica camera 50MP, 120W HyperCharge siêu nhanh.", Price = 19990000, ImageUrl = placeholder, CategoryId = phoneCat.CategoryId, SupplierId = samsung.SupplierId, IsNew = true, IsHot = false, IsDeal = false },

                // --- LAPTOP ---
                new Product { Name = "MacBook Pro 14 inch M3 Pro", Description = "Chip M3 Pro 12-core CPU, 18-core GPU, 18GB RAM, 512GB SSD, Liquid Retina XDR, pin 17h.", Price = 49990000, ImageUrl = placeholder, CategoryId = laptopCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true, IsHot = true, IsDeal = false },
                new Product { Name = "Dell XPS 15 9530", Description = "Intel Core i9-13900H, RTX 4070, 32GB RAM, 1TB SSD, 15.6 inch 3.5K OLED Touch.", Price = 69990000, ImageUrl = placeholder, CategoryId = laptopCat.CategoryId, SupplierId = dell.SupplierId, IsNew = false, IsHot = false, IsDeal = true },
                new Product { Name = "MacBook Air 15 M3", Description = "Chip M3, 15.3 inch Liquid Retina, 18GB RAM, 256GB SSD, pin 18h.", Price = 34990000, ImageUrl = placeholder, CategoryId = laptopCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true, IsHot = true, IsDeal = false },
                new Product { Name = "Dell Inspiron 15 3530", Description = "Intel Core i5-1335U, 8GB RAM, 512GB SSD, 15.6 inch FHD IPS.", Price = 15990000, ImageUrl = placeholder, CategoryId = laptopCat.CategoryId, SupplierId = dell.SupplierId, IsNew = false, IsHot = false, IsDeal = true },
                new Product { Name = "ASUS ROG Zephyrus G14", Description = "AMD Ryzen 9 7940HS, RTX 4070, 16GB RAM, 1TB SSD, 14 inch 165Hz.", Price = 54990000, ImageUrl = placeholder, CategoryId = laptopCat.CategoryId, SupplierId = dell.SupplierId, IsNew = false, IsHot = true, IsDeal = false },
                new Product { Name = "HP Pavilion 15", Description = "Intel Core i7-1355U, 16GB RAM, 512GB SSD, 15.6 inch IPS FHD.", Price = 21990000, ImageUrl = placeholder, CategoryId = laptopCat.CategoryId, SupplierId = dell.SupplierId, IsNew = false, IsHot = false, IsDeal = true },
                new Product { Name = "Lenovo ThinkPad X1 Carbon", Description = "Intel Core i7-1365U, 16GB RAM, 512GB SSD, 14 inch 2.8K OLED.", Price = 49990000, ImageUrl = placeholder, CategoryId = laptopCat.CategoryId, SupplierId = dell.SupplierId, IsNew = false, IsHot = false, IsDeal = false },

                // --- TABLET ---
                new Product { Name = "iPad Pro 12.9 inch M2", Description = "M2 chip, 12.9-inch Liquid Retina XDR, 256GB, WiFi + 5G, hỗ trợ Apple Pencil Gen 2.", Price = 32990000, ImageUrl = placeholder, CategoryId = tabletCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true, IsHot = false, IsDeal = false },
                new Product { Name = "Samsung Galaxy Tab S9 Ultra", Description = "Snapdragon 8 Gen 2, 14.6 inch AMOLED 120Hz, S Pen included, 256GB.", Price = 28990000, ImageUrl = placeholder, CategoryId = tabletCat.CategoryId, SupplierId = samsung.SupplierId, IsNew = true, IsHot = true, IsDeal = true },

                // --- ÂM THANH ---
                new Product { Name = "Sony WH-1000XM5", Description = "Tai nghe chống ồn cao cấp, 30 giờ pin, LDAC, 8 micro khử ồn AI.", Price = 9990000, ImageUrl = placeholder, CategoryId = audioCat.CategoryId, SupplierId = sony.SupplierId, IsNew = true, IsHot = true, IsDeal = false },
                new Product { Name = "AirPods Pro 2", Description = "Chip H2, Active Noise Cancellation, Adaptive Audio, USB-C, 6h pin.", Price = 6490000, ImageUrl = placeholder, CategoryId = audioCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true, IsHot = true, IsDeal = true },
                new Product { Name = "Sony WF-1000XM5", Description = "Tai nghe True Wireless chống ồn, 8h pin, LDAC, Multipoint 2 thiết bị.", Price = 7490000, ImageUrl = placeholder, CategoryId = audioCat.CategoryId, SupplierId = sony.SupplierId, IsNew = false, IsHot = false, IsDeal = true },

                // --- PHỤ KIỆN ---
                new Product { Name = "LG UltraGear 27GR95QE-B", Description = "Màn hình gaming 27 inch OLED, 2K QHD, 240Hz, 0.03ms GtG, G-Sync.", Price = 19990000, ImageUrl = placeholder, CategoryId = accessoryCat.CategoryId, SupplierId = lg.SupplierId, IsNew = false, IsHot = true, IsDeal = false },
                new Product { Name = "Logitech MX Master 3S", Description = "Chuột không dây cao cấp, 8K DPI, scroll电磁, kết nối 3 thiết bị.", Price = 2490000, ImageUrl = placeholder, CategoryId = accessoryCat.CategoryId, SupplierId = dell.SupplierId, IsNew = false, IsHot = false, IsDeal = true },
                new Product { Name = "Apple Watch Series 9 45mm", Description = "Chip S9, màn hình Always-On Retina, theo dõi sức khỏe toàn diện.", Price = 11990000, ImageUrl = placeholder, CategoryId = accessoryCat.CategoryId, SupplierId = apple.SupplierId, IsNew = true, IsHot = true, IsDeal = false },
                new Product { Name = "Samsung Galaxy Watch 6 Classic", Description = "Màn hình Super AMOLED 47mm, bezel xoay, theo dõi sức khỏe, LTE.", Price = 9990000, ImageUrl = placeholder, CategoryId = accessoryCat.CategoryId, SupplierId = samsung.SupplierId, IsNew = true, IsHot = false, IsDeal = true },

                // --- THIẾT BỊ MẠNG ---
                new Product { Name = "TP-Link Deco XE75 Pro", Description = "Hệ thống WiFi 6E Mesh, 3 pack, tốc độ 5.4Gbps, AI-Driven.", Price = 7990000, ImageUrl = placeholder, CategoryId = networkCat.CategoryId, SupplierId = dell.SupplierId, IsNew = false, IsHot = false, IsDeal = true },
                new Product { Name = "ASUS RT-AX88U Pro", Description = "Router WiFi 6, 8 cổng LAN, tốc độ 6000Mbps, game boost.", Price = 5990000, ImageUrl = placeholder, CategoryId = networkCat.CategoryId, SupplierId = dell.SupplierId, IsNew = false, IsHot = false, IsDeal = false },

                // --- MÁY ẢNH ---
                new Product { Name = "Sony A7 IV Body", Description = "Hybrid full-frame 33MP, 4K 60fps, Eye-AF, IBIS, 759 điểm lấy nét.", Price = 55990000, ImageUrl = placeholder, CategoryId = cameraCat.CategoryId, SupplierId = sony.SupplierId, IsNew = false, IsHot = true, IsDeal = false },
                new Product { Name = "Canon EOS R6 Mark II", Description = "Full-frame 24.2MP, 4K 60fps, 12fps cơ, IBIS, lấy nét AI.", Price = 62990000, ImageUrl = placeholder, CategoryId = cameraCat.CategoryId, SupplierId = sony.SupplierId, IsNew = true, IsHot = false, IsDeal = false },

                // --- LINH KIỆN ---
                new Product { Name = "Samsung 990 Pro 2TB NVMe SSD", Description = "PCIe 4.0 NVMe, tốc độ đọc 7450MB/s, tốc độ ghi 6900MB/s.", Price = 4990000, ImageUrl = placeholder, CategoryId = componentCat.CategoryId, SupplierId = samsung.SupplierId, IsNew = true, IsHot = true, IsDeal = true },
                new Product { Name = "Corsair Vengeance 32GB DDR5 5600", Description = "RAM DDR5 32GB (2x16GB), 5600MHz, CL36, RGB, bảo hành trọn đời.", Price = 3990000, ImageUrl = placeholder, CategoryId = componentCat.CategoryId, SupplierId = dell.SupplierId, IsNew = false, IsHot = false, IsDeal = true },
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            // Tạo inventory cho tất cả sản phẩm
            var productList = await context.Products.ToListAsync();
            var inventories = productList.Select(p => new Inventory
            {
                ProductId = p.ProductId,
                QuantityInStock = 100,
                LastUpdatedDate = DateTime.Now
            }).ToList();

            context.Inventory.AddRange(inventories);
            await context.SaveChangesAsync();
        }

        private static async Task RepairCorruptedTextAsync(ApplicationDbContext context)
        {
            var changed = false;

            if (context.Database.GetDbConnection() is not SqlConnection connection)
            {
                return;
            }

            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            var canonicalCategoryNames = new Dictionary<int, string>
            {
                [1] = "Điện thoại",
                [2] = "Laptop",
                [3] = "Tablet",
                [4] = "Phụ kiện",
                [5] = "Đồng hồ thông minh",
                [6] = "Gaming"
            };

            try
            {
                changed |= await RepairCategoriesAsync(connection, canonicalCategoryNames);
                changed |= await RepairSuppliersAsync(connection);
                changed |= await RepairProductsAsync(connection);

                if (changed)
                {
                    await context.SaveChangesAsync();
                }
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }
        }

        private static async Task<bool> RepairCategoriesAsync(SqlConnection connection, IReadOnlyDictionary<int, string> canonicalCategoryNames)
        {
            var changed = false;
            using var select = new SqlCommand("SELECT category_id, name FROM Categories", connection);
            using var reader = await select.ExecuteReaderAsync();
            var rows = new List<(int Id, string? Name)>();

            while (await reader.ReadAsync())
            {
                rows.Add((reader.GetInt32(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
            }

            await reader.CloseAsync();

            foreach (var row in rows)
            {
                var normalized = ProductDescriptionText.NormalizePlainText(row.Name) ?? string.Empty;
                if (canonicalCategoryNames.TryGetValue(row.Id, out var canonical)
                    && (normalized.Contains('\uFFFD') || normalized.Length == 0 || normalized == row.Name))
                {
                    normalized = canonical;
                }

                if (!string.Equals(row.Name, normalized, StringComparison.Ordinal))
                {
                    await UpdateTextAsync(connection, "Categories", "category_id", "name", row.Id, normalized);
                    changed = true;
                }
            }

            return changed;
        }

        private static async Task<bool> RepairSuppliersAsync(SqlConnection connection)
        {
            var changed = false;
            using var select = new SqlCommand("SELECT supplier_id, name, contact_person, address FROM Suppliers", connection);
            using var reader = await select.ExecuteReaderAsync();
            var rows = new List<(int Id, string? Name, string? Contact, string? Address)>();

            while (await reader.ReadAsync())
            {
                rows.Add((
                    reader.GetInt32(0),
                    reader.IsDBNull(1) ? null : reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3)));
            }

            await reader.CloseAsync();

            foreach (var row in rows)
            {
                var normalizedName = ProductDescriptionText.NormalizePlainText(row.Name) ?? string.Empty;
                var normalizedContact = ProductDescriptionText.NormalizePlainText(row.Contact);
                var normalizedAddress = ProductDescriptionText.NormalizePlainText(row.Address);

                if (!string.Equals(row.Name, normalizedName, StringComparison.Ordinal))
                {
                    await UpdateTextAsync(connection, "Suppliers", "supplier_id", "name", row.Id, normalizedName);
                    changed = true;
                }

                if (!string.Equals(row.Contact, normalizedContact, StringComparison.Ordinal))
                {
                    await UpdateTextAsync(connection, "Suppliers", "supplier_id", "contact_person", row.Id, normalizedContact);
                    changed = true;
                }

                if (!string.Equals(row.Address, normalizedAddress, StringComparison.Ordinal))
                {
                    await UpdateTextAsync(connection, "Suppliers", "supplier_id", "address", row.Id, normalizedAddress);
                    changed = true;
                }
            }

            return changed;
        }

        private static async Task<bool> RepairProductsAsync(SqlConnection connection)
        {
            var changed = false;
            var canonicalProducts = await LoadCanonicalProductsAsync();
            using var select = new SqlCommand("SELECT product_id, name, description FROM Products", connection);
            using var reader = await select.ExecuteReaderAsync();
            var rows = new List<(int Id, string? Name, string? Description)>();

            while (await reader.ReadAsync())
            {
                rows.Add((
                    reader.GetInt32(0),
                    reader.IsDBNull(1) ? null : reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2)));
            }

            await reader.CloseAsync();

            foreach (var row in rows)
            {
                var normalizedName = ProductDescriptionText.NormalizePlainText(row.Name) ?? string.Empty;
                var normalizedDescription = ProductDescriptionText.SanitizeDescriptionHtmlNullable(row.Description);
                var normalizedImageUrl = (string?)null;

                if (canonicalProducts.TryGetValue(row.Id, out var canonical))
                {
                    if (!string.IsNullOrWhiteSpace(canonical.Name))
                    {
                        normalizedName = ProductDescriptionText.NormalizePlainText(canonical.Name) ?? normalizedName;
                    }

                    if (!string.IsNullOrWhiteSpace(canonical.Description))
                    {
                        normalizedDescription = ProductDescriptionText.SanitizeDescriptionHtmlNullable(canonical.Description);
                    }

                    normalizedImageUrl = NormalizeImageUrl(canonical.ImageUrl ?? canonical.Image);
                }

                normalizedImageUrl ??= NormalizeImageUrl(null);

                if (!string.Equals(row.Name, normalizedName, StringComparison.Ordinal))
                {
                    await UpdateTextAsync(connection, "Products", "product_id", "name", row.Id, normalizedName);
                    changed = true;
                }

                if (!string.Equals(row.Description, normalizedDescription, StringComparison.Ordinal))
                {
                    await UpdateTextAsync(connection, "Products", "product_id", "description", row.Id, normalizedDescription);
                    changed = true;
                }

                if (await UpdateImageUrlAsync(connection, row.Id, normalizedImageUrl))
                {
                    changed = true;
                }
            }

            return changed;
        }

        private static async Task<Dictionary<int, CanonicalProduct>> LoadCanonicalProductsAsync()
        {
            var candidatePaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "sample_products.json"),
                Path.Combine(AppContext.BaseDirectory, "sample_products.json"),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "sample_products.json"))
            };

            var filePath = candidatePaths.FirstOrDefault(File.Exists);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return new Dictionary<int, CanonicalProduct>();
            }

            var json = await File.ReadAllTextAsync(filePath);
            var products = System.Text.Json.JsonSerializer.Deserialize<List<CanonicalProduct>>(json, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (products == null)
            {
                return new Dictionary<int, CanonicalProduct>();
            }

            return products
                .Where(p => p.Id > 0)
                .ToDictionary(p => p.Id, p => p);
        }

        private sealed class CanonicalProduct
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public string? Image { get; set; }
            public string? ImageUrl { get; set; }
        }

        private static string NormalizeImageUrl(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return "/images/products/placeholder.svg";
            }

            if (imageUrl.Contains("via.placeholder.com", StringComparison.OrdinalIgnoreCase))
            {
                return "/images/products/placeholder.svg";
            }

            return imageUrl.Trim();
        }

        private static async Task<bool> UpdateImageUrlAsync(SqlConnection connection, int id, string imageUrl)
        {
            using var update = new SqlCommand("UPDATE Products SET image_url = @imageUrl WHERE product_id = @id AND (image_url IS NULL OR image_url <> @imageUrl)", connection);
            update.Parameters.AddWithValue("@id", id);
            update.Parameters.AddWithValue("@imageUrl", imageUrl);
            return await update.ExecuteNonQueryAsync() > 0;
        }

        private static async Task UpdateTextAsync(SqlConnection connection, string tableName, string keyColumn, string textColumn, int id, string? value)
        {
            using var update = new SqlCommand($"UPDATE {tableName} SET {textColumn} = @value WHERE {keyColumn} = @id", connection);
            update.Parameters.AddWithValue("@id", id);
            update.Parameters.AddWithValue("@value", (object?)value ?? DBNull.Value);
            await update.ExecuteNonQueryAsync();
        }
    }
}

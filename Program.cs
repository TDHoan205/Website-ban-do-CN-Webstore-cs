using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Webstore.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Webstore.Services;
using Webstore.Services.AI;
using Microsoft.Extensions.Caching.Memory;

namespace Webstore
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Read payment info from config
            var paymentSection = builder.Configuration.GetSection("PaymentInfo");
            var paymentInfo = new
            {
                BankName = paymentSection["BankName"] ?? "Vietcombank",
                AccountNumber = paymentSection["AccountNumber"] ?? "123456789012",
                AccountName = paymentSection["AccountName"] ?? "WEBSTORE SHOP"
            };

            // Add Memory Cache for performance
            builder.Services.AddMemoryCache(options =>
            {
                options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
            });

            // Add Response Caching
            builder.Services.AddResponseCaching(options =>
            {
                options.MaximumBodySize = 1024 * 1024; // 1MB
                options.UseCaseSensitivePaths = false;
            });

            // Add services to the container.
            builder.Services.AddControllersWithViews()
                .AddMvcOptions(options =>
                {
                    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
                })
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                });
            builder.Services.Configure<Microsoft.Extensions.Configuration.ConfigurationManager>(options =>
            {
                // Pass payment info to all views via ViewData
            });

            // Add session support
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // EF Core DbContext
            builder.Services.AddDbContext<Webstore.Data.ApplicationDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions => sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

            // Cookie Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Auth/Login";
                    options.LogoutPath = "/Auth/Logout";
                    options.AccessDeniedPath = "/Auth/Login";
                    options.Cookie.HttpOnly = true;
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                });

            // Register AI Services - Gemini
            builder.Services.AddSingleton<IGeminiService, GeminiService>();
            builder.Services.AddScoped<Webstore.Services.AI.EmbeddingService>();
            builder.Services.AddScoped<Webstore.Services.AI.KnowledgeBaseService>();
            builder.Services.AddScoped<Webstore.Services.AI.IntentDetectionService>();
            builder.Services.AddScoped<Webstore.Services.AI.RAGEngineService>();
            builder.Services.AddScoped<Webstore.Services.AI.AIResponseService>();
            builder.Services.AddScoped<IToolDispatcherService, GeminiToolDispatcherService>();
            builder.Services.AddScoped<IAIAgentService, AIAgentService>();
            builder.Services.AddScoped<IOrchestratorService, OrchestratorService>();

            // Register Repositories
            builder.Services.AddScoped(typeof(Webstore.Data.Repositories.IRepository<>), typeof(Webstore.Data.Repositories.GenericRepository<>));
            builder.Services.AddScoped<Webstore.Data.Repositories.IProductRepository, Webstore.Data.Repositories.ProductRepository>();
            builder.Services.AddScoped<Webstore.Data.Repositories.IOrderRepository, Webstore.Data.Repositories.OrderRepository>();
            builder.Services.AddScoped<Webstore.Data.Repositories.ICategoryRepository, Webstore.Data.Repositories.CategoryRepository>();
            builder.Services.AddScoped<Webstore.Data.Repositories.ISupplierRepository, Webstore.Data.Repositories.SupplierRepository>();
            builder.Services.AddScoped<Webstore.Data.Repositories.IInventoryRepository, Webstore.Data.Repositories.InventoryRepository>();
            builder.Services.AddScoped<Webstore.Data.Repositories.IAccountRepository, Webstore.Data.Repositories.AccountRepository>();
            builder.Services.AddScoped<Webstore.Data.Repositories.IEmployeeRepository, Webstore.Data.Repositories.EmployeeRepository>();

            // Register Shop Services
            builder.Services.AddScoped<Webstore.Services.IProductService, Webstore.Services.ProductService>();
            builder.Services.AddScoped<Webstore.Services.ICartService, Webstore.Services.CartService>();
            builder.Services.AddScoped<Webstore.Services.IOrderService, Webstore.Services.OrderService>();
            builder.Services.AddScoped<Webstore.Services.IInventoryService, Webstore.Services.InventoryService>();
            builder.Services.AddScoped<Webstore.Services.ISupplierService, Webstore.Services.SupplierService>();
            builder.Services.AddScoped<Webstore.Services.IStatisticsService, Webstore.Services.StatisticsService>();
            builder.Services.AddScoped<Webstore.Services.IAccountService, Webstore.Services.AccountService>();
            builder.Services.AddScoped<Webstore.Services.ICategoryService, Webstore.Services.CategoryService>();
            builder.Services.AddSingleton<IEmailService, Webstore.Services.EmailService>();

            var app = builder.Build();

            // Seed data
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<Webstore.Data.ApplicationDbContext>();
                try
                {
                    if (db.Database.CanConnect())
                    {
                        Console.WriteLine("✅ Kết nối database thành công!");

                        // Đảm bảo database được tạo
                        db.Database.EnsureCreated();

                        // Ensure OrderDetails table exists. If the database already exists EnsureCreated
                        // won't create new tables, so run a safe IF NOT EXISTS ... CREATE TABLE for OrderDetails.
                        var createOrderDetailsIfMissing = @"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OrderDetails]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[OrderDetails](
        [OrderID] [int] NOT NULL,
        [ProductID] [int] NOT NULL,
        [Quantity] [int] NOT NULL,
        [Price] [decimal](18, 2) NOT NULL,
     CONSTRAINT [PK_OrderDetails] PRIMARY KEY CLUSTERED 
    (
        [OrderID] ASC,
        [ProductID] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY];
END";

                        try
                        {
                            // Execute the DDL to create the table only if it's missing
                            await db.Database.ExecuteSqlRawAsync(createOrderDetailsIfMissing);
                            Console.WriteLine("✅ Đảm bảo bảng OrderDetails tồn tại (nếu cần đã tạo).");
                        }
                        catch (Exception exTable)
                        {
                            Console.WriteLine($"⚠️ Không thể đảm bảo bảng OrderDetails: {exTable.Message}");
                        }

                        var createKnowledgeChunksIfMissing = @"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[KnowledgeChunks]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[KnowledgeChunks](
        [chunk_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [source_type] NVARCHAR(20) NOT NULL,
        [source_id] INT NOT NULL,
        [chunk_type] NVARCHAR(30) NOT NULL,
        [raw_text] NVARCHAR(MAX) NOT NULL,
        [normalized_text] NVARCHAR(MAX) NOT NULL,
        [embedding] NVARCHAR(MAX) NOT NULL,
        [price] DECIMAL(10,2) NULL,
        [category] NVARCHAR(100) NULL,
        [priority] INT NOT NULL DEFAULT 0,
        [created_at] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_KnowledgeChunks_Source' AND object_id = OBJECT_ID(N'[dbo].[KnowledgeChunks]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_KnowledgeChunks_Source] ON [dbo].[KnowledgeChunks]([source_type], [source_id]);
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_KnowledgeChunks_Category' AND object_id = OBJECT_ID(N'[dbo].[KnowledgeChunks]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_KnowledgeChunks_Category] ON [dbo].[KnowledgeChunks]([category]);
END";

                        try
                        {
                            await db.Database.ExecuteSqlRawAsync(createKnowledgeChunksIfMissing);
                            Console.WriteLine("✅ Đảm bảo bảng KnowledgeChunks tồn tại (nếu cần đã tạo).");
                        }
                        catch (Exception exKbTable)
                        {
                            Console.WriteLine($"⚠️ Không thể đảm bảo bảng KnowledgeChunks: {exKbTable.Message}");
                        }

                        // Tạo bảng Receipts_Shipments nếu chưa tồn tại
                        var createReceiptsShipmentsIfMissing = @"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Receipts_Shipments]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Receipts_Shipments](
        [movement_id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [product_id] [int] NOT NULL,
        [movement_type] [nvarchar](10) NOT NULL,
        [quantity] [int] NOT NULL,
        [movement_date] [datetime2] NOT NULL DEFAULT GETDATE(),
        [related_order_id] [int] NULL,
        CONSTRAINT [FK_Receipts_Shipments_Products] FOREIGN KEY ([product_id]) REFERENCES [Products]([product_id])
    );
END";

                        try
                        {
                            await db.Database.ExecuteSqlRawAsync(createReceiptsShipmentsIfMissing);
                            Console.WriteLine("✅ Đảm bảo bảng Receipts_Shipments tồn tại.");
                        }
                        catch (Exception exRsTable)
                        {
                            Console.WriteLine($"⚠️ Không thể đảm bảo bảng Receipts_Shipments: {exRsTable.Message}");
                        }

                        // Tạo cột is_available cho Products nếu chưa tồn tại
                        var addIsAvailableColumn = @"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Object_ID = Object_ID('[dbo].[Products]') AND name = 'is_available')
BEGIN
    ALTER TABLE [dbo].[Products] ADD [is_available] BIT NOT NULL DEFAULT 1;
END";
                        try
                        {
                            await db.Database.ExecuteSqlRawAsync(addIsAvailableColumn);
                            Console.WriteLine("✅ Đảm bảo cột is_available tồn tại trong bảng Products.");
                        }
                        catch (Exception exIsAvail)
                        {
                            Console.WriteLine($"⚠️ Không thể thêm cột is_available: {exIsAvail.Message}");
                        }

                        // Tạo cột reset_token và reset_token_expiry cho Accounts nếu chưa tồn tại
                        var addResetTokenColumns = @"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Object_ID = Object_ID('[dbo].[Accounts]') AND name = 'reset_token')
BEGIN
    ALTER TABLE [dbo].[Accounts] ADD [reset_token] NVARCHAR(64) NULL;
END
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Object_ID = Object_ID('[dbo].[Accounts]') AND name = 'reset_token_expiry')
BEGIN
    ALTER TABLE [dbo].[Accounts] ADD [reset_token_expiry] DATETIME NULL;
END";
                        try
                        {
                            await db.Database.ExecuteSqlRawAsync(addResetTokenColumns);
                            Console.WriteLine("✅ Đảm bảo các cột ResetToken tồn tại trong bảng Accounts.");
                        }
                        catch (Exception exResetToken)
                        {
                            Console.WriteLine($"⚠️ Không thể thêm các cột ResetToken: {exResetToken.Message}");
                        }

                        // Migration: Make Orders.account_id nullable (guest checkout support)
                        try
                        {
                            var fixAccountIdMigration = @"
                                BEGIN TRY
                                    DECLARE @fkName NVARCHAR(128);
                                    SELECT @fkName = fk.name
                                    FROM sys.foreign_keys fk
                                    INNER JOIN sys.tables t ON fk.parent_object_id = t.object_id
                                    WHERE t.name = 'Orders' AND fk.parent_object_id = OBJECT_ID('Orders');
                                    IF @fkName IS NOT NULL
                                    BEGIN
                                        EXEC('ALTER TABLE Orders DROP CONSTRAINT [' + @fkName + ']');
                                        PRINT 'Dropped FK on Orders.account_id';
                                    END
                                    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'account_id' AND is_nullable = 0)
                                    BEGIN
                                        ALTER TABLE Orders ALTER COLUMN account_id INT NULL;
                                        PRINT 'Made Orders.account_id nullable';
                                    END
                                END TRY BEGIN CATCH END CATCH";
                            await db.Database.ExecuteSqlRawAsync(fixAccountIdMigration);
                            Console.WriteLine("✅ Migration: Orders.account_id is now nullable (guest checkout).");
                        }
                        catch (Exception exMigration)
                        {
                            Console.WriteLine($"⚠️ Migration warning: {exMigration.Message}");
                        }

                        // Seed dữ liệu mẫu
                        await SeedData.SeedAsync(db);
                        Console.WriteLine("✅ Đã thêm dữ liệu mẫu vào database!");

                        try
                        {
                            var knowledgeBase = scope.ServiceProvider.GetRequiredService<KnowledgeBaseService>();
                            await knowledgeBase.BuildOrRefreshAsync();
                            Console.WriteLine("✅ Đã xây dựng Knowledge Base vector cho AI Chat.");
                        }
                        catch (Exception exKbBuild)
                        {
                            Console.WriteLine($"⚠️ Không thể xây dựng Knowledge Base: {exKbBuild.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ Không thể kết nối database. Kiểm tra connection string!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Lỗi khi kết nối database:");
                    Console.WriteLine($"Main Error: {ex.Message}");
                    var inner = ex.InnerException;
                    while (inner != null)
                    {
                        Console.WriteLine($"Inner Error: {inner.Message}");
                        inner = inner.InnerException;
                    }
                    Console.WriteLine("Full StackTrace:");
                    Console.WriteLine(ex.ToString());
                }
            }


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseStaticFiles();

            // Response Caching for performance
            app.UseResponseCaching();

            app.UseRouting();

            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Shop}/{action=Index}/{id?}");

            // API endpoint tạm thời để reset dữ liệu (xóa sau khi dùng)
            app.MapGet("/api/reset-data", async (Webstore.Data.ApplicationDbContext db) =>
            {
                try
                {
                    // Xóa dữ liệu theo thứ tự để tránh vi phạm khóa ngoại
                    db.ChatMessages.RemoveRange(db.ChatMessages);
                    db.ChatSessions.RemoveRange(db.ChatSessions);
                    db.AIConversationLogs.RemoveRange(db.AIConversationLogs);
                    db.KnowledgeChunks.RemoveRange(db.KnowledgeChunks);
                    db.OrderItems.RemoveRange(db.OrderItems);
                    db.Orders.RemoveRange(db.Orders);
                    db.CartItems.RemoveRange(db.CartItems);
                    db.ReceiptShipments.RemoveRange(db.ReceiptShipments);
                    db.Inventory.RemoveRange(db.Inventory);
                    db.Employees.RemoveRange(db.Employees);
                    db.FAQs.RemoveRange(db.FAQs);
                    db.Notifications.RemoveRange(db.Notifications);
                    db.ProductVariants.RemoveRange(db.ProductVariants);
                    db.Products.RemoveRange(db.Products);
                    db.Categories.RemoveRange(db.Categories);
                    db.Suppliers.RemoveRange(db.Suppliers);
                    db.Accounts.RemoveRange(db.Accounts);
                    await db.SaveChangesAsync();

                    // Tạo bảng Receipts_Shipments nếu chưa tồn tại
                    await db.Database.ExecuteSqlRawAsync(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Receipts_Shipments]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Receipts_Shipments](
        [movement_id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [product_id] [int] NOT NULL,
        [movement_type] [nvarchar](10) NOT NULL,
        [quantity] [int] NOT NULL,
        [movement_date] [datetime2] NOT NULL DEFAULT GETDATE(),
        [related_order_id] [int] NULL
    );
END");

                    // Seed lại dữ liệu mới
                    await SeedData.SeedAsync(db);

                    return Results.Ok(new { success = true, message = "Đã reset dữ liệu thành công! Tải lại trang." });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { success = false, message = ex.Message });
                }
            });

            app.Run();
        }
    }
}

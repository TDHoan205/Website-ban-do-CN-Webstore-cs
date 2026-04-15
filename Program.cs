using Microsoft.EntityFrameworkCore;
using Webstore.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Webstore.Services.AI;

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

            // Add services to the container.
            builder.Services.AddControllersWithViews()
                .AddMvcOptions(options =>
                {
                    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
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

            // Register AI Services
            builder.Services.AddScoped<IntentDetectionService>();
            builder.Services.AddScoped<RAGEngineService>();
            builder.Services.AddScoped<AIResponseService>();

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

                        // Seed dữ liệu mẫu
                        await SeedData.SeedAsync(db);
                        Console.WriteLine("✅ Đã thêm dữ liệu mẫu vào database!");
                    }
                    else
                    {
                        Console.WriteLine("❌ Không thể kết nối database. Kiểm tra connection string!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Lỗi khi kết nối database: {ex.Message}");
                }
            }


            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Shop}/{action=Index}/{id?}");

            app.Run();
        }
    }
}

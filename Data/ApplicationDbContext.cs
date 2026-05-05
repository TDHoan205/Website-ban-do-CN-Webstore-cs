using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Webstore.Models;
using Webstore.Models.AI;
using Webstore.Utilities;

namespace Webstore.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<Inventory> Inventory => Set<Inventory>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<ReceiptShipment> ReceiptShipments => Set<ReceiptShipment>();
        public DbSet<ProductImage> ProductImages => Set<ProductImage>();

        // AI Chat Models
        public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<FAQ> FAQs => Set<FAQ>();
        public DbSet<AIConversationLog> AIConversationLogs => Set<AIConversationLog>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<KnowledgeChunk> KnowledgeChunks => Set<KnowledgeChunk>();
        public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var plainTextConverter = new ValueConverter<string, string>(
                v => ProductDescriptionText.NormalizePlainText(v) ?? string.Empty,
                v => ProductDescriptionText.NormalizePlainText(v) ?? string.Empty);

            var descriptionHtmlConverter = new ValueConverter<string, string>(
                v => ProductDescriptionText.SanitizeDescriptionHtmlNullable(v) ?? string.Empty,
                v => ProductDescriptionText.SanitizeDescriptionHtmlNullable(v) ?? string.Empty);

            // Unique index for Category.Name
            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            // Unique constraints mapping
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.AccountId)
                .IsUnique();

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.EmployeeCode)
                .IsUnique();

            modelBuilder.Entity<Inventory>()
                .HasIndex(i => i.ProductId)
                .IsUnique();

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.RoleName)
                .IsUnique();

            modelBuilder.Entity<Cart>()
                .HasIndex(c => c.AccountId)
                .IsUnique();

            modelBuilder.Entity<Cart>()
                .HasIndex(c => c.SessionId)
                .IsUnique();

            modelBuilder.Entity<CartItem>()
                .HasIndex(ci => new { ci.CartId, ci.ProductId, ci.VariantId })
                .IsUnique();

            modelBuilder.Entity<ProductImage>()
                .HasIndex(pi => new { pi.ProductId, pi.VariantId, pi.DisplayOrder });

            modelBuilder.Entity<ProductImage>()
                .HasIndex(pi => new { pi.ProductId, pi.IsPrimary });

            modelBuilder.Entity<ProductImage>()
                .HasIndex(pi => new { pi.VariantId, pi.DisplayOrder });

            modelBuilder.Entity<ProductVariant>()
                .HasIndex(pv => new { pv.ProductId, pv.Color, pv.Storage, pv.RAM });

            // Enum-like constraints (Role, Status, MovementType) can be validated at service/controller level.
            // Configure decimal precision where necessary
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(10,2)");

            // Thêm cấu hình cho ImageUrl (nếu cần)
            modelBuilder.Entity<Product>()
                .Property(p => p.ImageUrl)
                .HasMaxLength(500);

            modelBuilder.Entity<Category>()
                .Property(c => c.Name)
                .HasConversion(new ValueConverter<string, string>(
                    v => ProductDescriptionText.NormalizePlainText(v ?? string.Empty) ?? string.Empty,
                    v => ProductDescriptionText.NormalizePlainText(v ?? string.Empty) ?? string.Empty));

            modelBuilder.Entity<Supplier>()
                .Property(s => s.Name)
                .HasConversion(new ValueConverter<string, string>(
                    v => ProductDescriptionText.NormalizePlainText(v ?? string.Empty) ?? string.Empty,
                    v => ProductDescriptionText.NormalizePlainText(v ?? string.Empty) ?? string.Empty));

            modelBuilder.Entity<Supplier>()
                .Property(s => s.ContactPerson)
                .HasConversion(new ValueConverter<string?, string?>(
                    v => v != null ? ProductDescriptionText.NormalizePlainText(v) : null,
                    v => v != null ? ProductDescriptionText.NormalizePlainText(v) : null));

            modelBuilder.Entity<Supplier>()
                .Property(s => s.Address)
                .HasConversion(new ValueConverter<string?, string?>(
                    v => v != null ? ProductDescriptionText.NormalizePlainText(v) : null,
                    v => v != null ? ProductDescriptionText.NormalizePlainText(v) : null));

            modelBuilder.Entity<Product>()
                .Property(p => p.Name)
                .HasConversion(new ValueConverter<string, string>(
                    v => ProductDescriptionText.NormalizePlainText(v) ?? string.Empty,
                    v => ProductDescriptionText.NormalizePlainText(v) ?? string.Empty));

            modelBuilder.Entity<Product>()
                .Property(p => p.Description)
                .HasConversion(new ValueConverter<string?, string?>(
                    v => v != null ? ProductDescriptionText.SanitizeDescriptionHtmlNullable(v) : null,
                    v => v != null ? ProductDescriptionText.SanitizeDescriptionHtmlNullable(v) : null));

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<AIConversationLog>()
                .Property(l => l.ConfidenceScore)
                .HasColumnType("decimal(5,4)");

            modelBuilder.Entity<KnowledgeChunk>()
                .Property(k => k.Price)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<KnowledgeChunk>()
                .HasIndex(k => new { k.SourceType, k.SourceId });

            modelBuilder.Entity<KnowledgeChunk>()
                .HasIndex(k => k.Category);
        }
    }
}


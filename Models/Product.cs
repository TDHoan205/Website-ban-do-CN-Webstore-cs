using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webstore.Models
{
    [Table("Products")]
    public class Product
    {
        [Key]
        [Column("product_id")]
        public int ProductId { get; set; }

        [Required]
        [StringLength(255)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("image_url")]
        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [Required]
        [Column("price", TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Column("original_price", TypeName = "decimal(10,2)")]
        public decimal? OriginalPrice { get; set; }

        [Column("stock_quantity")]
        public int StockQuantity { get; set; } = 0;

        [Column("rating", TypeName = "decimal(2,1)")]
        public decimal Rating { get; set; } = 4.5m;

        [Column("is_new")]
        public bool IsNew { get; set; } = false;

        [Column("is_hot")]
        public bool IsHot { get; set; } = false;

        [Column("discount_percent")]
        public int DiscountPercent { get; set; } = 0;

        /// <summary>JSON string chứa thông số kỹ thuật cơ bản của sản phẩm</summary>
        [Column("specifications")]
        public string? Specifications { get; set; }

        [Column("category_id")]
        public int? CategoryId { get; set; }

        [Column("supplier_id")]
        public int? SupplierId { get; set; }

        // Computed: không lưu DB, tính từ Variants hoặc Discount
        [NotMapped]
        public bool IsDeal => DiscountPercent > 0 || (OriginalPrice.HasValue && OriginalPrice > Price);

        // Navigation properties
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        [ForeignKey("SupplierId")]
        public virtual Supplier? Supplier { get; set; }

        public virtual Inventory? Inventory { get; set; }
        public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<ReceiptShipment> ReceiptShipments { get; set; } = new List<ReceiptShipment>();
    }
}

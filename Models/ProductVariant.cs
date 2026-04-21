using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webstore.Models
{
    /// <summary>
    /// Biến thể sản phẩm - đồng bộ naming RAM
    /// </summary>
    [Table("ProductVariants")]
    public class ProductVariant
    {
        [Key]
        [Column("variant_id")]
        public int VariantId { get; set; }

        [Required]
        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("color")]
        [StringLength(50)]
        public string? Color { get; set; }

        [Column("storage")]
        [StringLength(20)]
        public string? Storage { get; set; }

        [Column("ram")]
        [StringLength(20)]
        public string? RAM { get; set; }

        [Column("variant_name")]
        [StringLength(100)]
        public string? VariantName { get; set; }

        [Column("price", TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Column("original_price", TypeName = "decimal(10,2)")]
        public decimal? OriginalPrice { get; set; }

        [Column("stock_quantity")]
        public int StockQuantity { get; set; } = 0;

        [Column("display_order")]
        public int DisplayOrder { get; set; } = 0;

        // Navigation
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webstore.Models
{
    [Table("Product_Images")]
    public class ProductImage
    {
        [Key]
        [Column("image_id")]
        public int ImageId { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("variant_id")]
        public int? VariantId { get; set; }

        [Required]
        [StringLength(500)]
        [Column("image_url")]
        public string ImageUrl { get; set; } = string.Empty;

        [Column("is_primary")]
        public bool IsPrimary { get; set; }

        [Column("display_order")]
        public int DisplayOrder { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [ForeignKey("VariantId")]
        public virtual ProductVariant? Variant { get; set; }
    }
}

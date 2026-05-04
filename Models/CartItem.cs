using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webstore.Models
{
    [Table("Cart_Items")]
    public class CartItem
    {
        [Key]
        [Column("cart_item_id")]
        public int CartItemId { get; set; }

        [Required]
        [Column("cart_id")]
        public int CartId { get; set; }

        [Required]
        [Column("product_id")]
        public int ProductId { get; set; }

        /// <summary>Biến thể được chọn (Màu + Dung lượng). Null = mua theo sản phẩm gốc</summary>
        [Column("variant_id")]
        public int? VariantId { get; set; }

        [Required]
        [Column("quantity")]
        [Range(1, 999)]
        public int Quantity { get; set; }

        [Column("added_date")]
        public DateTime AddedDate { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("CartId")]
        public virtual Cart? Cart { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [ForeignKey("VariantId")]
        public virtual ProductVariant? Variant { get; set; }
    }
}

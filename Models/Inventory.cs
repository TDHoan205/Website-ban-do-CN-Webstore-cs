using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webstore.Models
{
    [Table("Inventory")]
    public class Inventory
    {
        [Key]
        [Column("inventory_id")]
        public int InventoryId { get; set; }

        [Required]
        [Column("product_id")]
        public int ProductId { get; set; }

        /// <summary>
        /// Số lượng tồn kho - Đồng bộ với thuộc tính StockQuantity
        /// </summary>
        [Required]
        [Column("quantity_in_stock")]
        public int StockQuantity { get; set; } = 0;

        /// <summary>
        /// Ngày cập nhật cuối - Đồng bộ với thuộc tính LastUpdated
        /// </summary>
        [Column("last_updated_date")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }
}

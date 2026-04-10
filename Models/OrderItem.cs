using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webstore.Models
{
    [Table("OrderDetails")]
    public class OrderItem
    {
        // Composite key (OrderID, ProductID) will be configured in DbContext fluent API

        [Required]
        [Column("OrderID")]
        public int OrderId { get; set; }

        [Required]
        [Column("ProductID")]
        public int ProductId { get; set; }

        [Required]
        [Column("Quantity")]
        public int Quantity { get; set; }

        // DB column is 'Price' (decimal). Map to UnitPrice property in model but point to Price column.
        [Required]
        [Column("Price", TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        // Navigation properties
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;
    }
}

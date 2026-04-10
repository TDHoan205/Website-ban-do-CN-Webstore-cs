using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webstore.Models
{
    [Table("Receipts_Shipments")]
    public class ReceiptShipment
    {
        [Key]
        [Column("movement_id")]
        public int MovementId { get; set; }

        [Required]
        [Column("product_id")]
        public int ProductId { get; set; }

        [Required]
        [StringLength(10)]
        [Column("movement_type")]
        public string MovementType { get; set; } = string.Empty;

        [Required]
        [Column("quantity")]
        public int Quantity { get; set; }

        [Required]
        [Column("movement_date")]
        public DateTime MovementDate { get; set; } = DateTime.Now;

        [Column("related_order_id")]
        public int? RelatedOrderId { get; set; }

        // Navigation properties
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;

        [ForeignKey("RelatedOrderId")]
        public virtual Order? RelatedOrder { get; set; }
    }
}

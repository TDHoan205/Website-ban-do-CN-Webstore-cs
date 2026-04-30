using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webstore.Models
{
    [Table("Orders")]
    public class Order
    {
        [Key]
        [Column("order_id")]
        public int OrderId { get; set; }

        [Column("account_id")]
        public int? AccountId { get; set; }

        [Required]
        [Column("order_date")]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        [Column("total_amount", TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(20)]
        [Column("status")]
        public string Status { get; set; } = "Pending";

        [StringLength(100)]
        [Column("customer_name")]
        public string? CustomerName { get; set; }

        [StringLength(20)]
        [Column("customer_phone")]
        public string? CustomerPhone { get; set; }

        [StringLength(255)]
        [Column("customer_address")]
        public string? CustomerAddress { get; set; }

        [StringLength(500)]
        [Column("notes")]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("AccountId")]
        public virtual Account? Account { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<ReceiptShipment> ReceiptShipments { get; set; } = new List<ReceiptShipment>();
    }
}

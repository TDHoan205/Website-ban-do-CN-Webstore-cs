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

        [Required]
        [Column("account_id")]
        public int AccountId { get; set; }

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

        // The following customer fields are not present in the current DB schema (Orders table)
        // Mark them NotMapped so EF won't expect those columns. If you prefer to store them,
        // add corresponding columns to the database or create a migration.
        [NotMapped]
        public string? CustomerName { get; set; }

        [NotMapped]
        public string? CustomerPhone { get; set; }

        [NotMapped]
        public string? CustomerAddress { get; set; }

        [NotMapped]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("AccountId")]
        public virtual Account Account { get; set; } = null!;

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<ReceiptShipment> ReceiptShipments { get; set; } = new List<ReceiptShipment>();
    }
}

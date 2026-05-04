using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webstore.Models
{
    [Table("Carts")]
    public class Cart
    {
        [Key]
        [Column("cart_id")]
        public int CartId { get; set; }

        [Column("account_id")]
        public int? AccountId { get; set; }

        [StringLength(64)]
        [Column("session_id")]
        public string? SessionId { get; set; }

        [StringLength(30)]
        [Column("role_name")]
        public string? RoleName { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("AccountId")]
        public virtual Account? Account { get; set; }

        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webstore.Models
{
    [Table("Accounts")]
    public class Account
    {
        [Key]
        [Column("account_id")]
        public int AccountId { get; set; }

        [Required]
        [StringLength(50)]
        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [StringLength(255)]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(100)]
        [Column("email")]
        public string? Email { get; set; }

        [StringLength(100)]
        [Column("full_name")]
        public string? FullName { get; set; }

        [StringLength(20)]
        [Column("phone")]
        public string? Phone { get; set; }

        [StringLength(255)]
        [Column("address")]
        public string? Address { get; set; }

        [Column("is_active")]
        public bool? IsActive { get; set; } = true;

        [Required]
        [StringLength(20)]
        [Column("role")]
        public string Role { get; set; } = "Customer";

        [StringLength(64)]
        [Column("reset_token")]
        public string? ResetToken { get; set; }

        [Column("reset_token_expiry")]
        public DateTime? ResetTokenExpiry { get; set; }

        // Navigation properties
        public virtual Employee? Employee { get; set; }
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}

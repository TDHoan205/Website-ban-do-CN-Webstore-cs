using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webstore.Models.AI
{
    /// <summary>
    /// Notification - thông báo cho user/admin
    /// </summary>
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        [Column("notification_id")]
        public int NotificationId { get; set; }

        [Column("account_id")]
        public int? AccountId { get; set; }

        [Required]
        [StringLength(50)]
        [Column("type")]
        public string Type { get; set; } = "general";

        [Required]
        [Column("message")]
        public string Message { get; set; } = string.Empty;

        [Column("is_read")]
        public bool IsRead { get; set; } = false;

        [StringLength(255)]
        [Column("link")]
        public string? Link { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("AccountId")]
        public virtual Account? Account { get; set; }
    }
}

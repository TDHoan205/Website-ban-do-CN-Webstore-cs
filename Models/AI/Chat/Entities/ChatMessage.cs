using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webstore.Models.AI
{
    /// <summary>
    /// Chat Message - tin nhắn trong phiên chat
    /// </summary>
    [Table("ChatMessages")]
    public class ChatMessage
    {
        [Key]
        [Column("message_id")]
        public int MessageId { get; set; }

        [Column("session_id")]
        public Guid SessionId { get; set; }

        [Required]
        [Column("message")]
        public string Message { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        [Column("sender_type")]
        public string SenderType { get; set; } = "user";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("metadata")]
        public string? Metadata { get; set; }

        [ForeignKey("SessionId")]
        public virtual ChatSession? Session { get; set; }
    }
}

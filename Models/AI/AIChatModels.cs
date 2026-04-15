using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webstore.Models.AI
{
    /// <summary>
    /// Chat Session - nhóm tin nhắn theo phiên
    /// </summary>
    [Table("ChatSessions")]
    public class ChatSession
    {
        [Key]
        [Column("session_id")]
        public Guid SessionId { get; set; } = Guid.NewGuid();

        [Column("account_id")]
        public int? AccountId { get; set; }

        [Column("status")]
        [StringLength(20)]
        public string Status { get; set; } = "active"; // active, escalated, closed

        [Column("assigned_to")]
        public int? AssignedTo { get; set; }

        [Column("started_at")]
        public DateTime StartedAt { get; set; } = DateTime.Now;

        [Column("ended_at")]
        public DateTime? EndedAt { get; set; }

        [ForeignKey("AccountId")]
        public virtual Account? Account { get; set; }
    }

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
        public string SenderType { get; set; } = "user"; // user, ai, admin

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("metadata")]
        public string? Metadata { get; set; } // JSON: intent, confidence, etc.

        [ForeignKey("SessionId")]
        public virtual ChatSession? Session { get; set; }
    }

    /// <summary>
    /// FAQ - Câu hỏi thường gặp
    /// </summary>
    [Table("FAQs")]
    public class FAQ
    {
        [Key]
        [Column("faq_id")]
        public int FaqId { get; set; }

        [Required]
        [Column("question")]
        public string Question { get; set; } = string.Empty;

        [Required]
        [Column("answer")]
        public string Answer { get; set; } = string.Empty;

        [StringLength(50)]
        [Column("category")]
        public string Category { get; set; } = "general"; // general, purchase, payment, warranty, shipping

        [Column("keywords")]
        public string? Keywords { get; set; } // comma-separated

        [Column("priority")]
        public int Priority { get; set; } = 5;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// AI Conversation Log - để đánh giá AI performance
    /// </summary>
    [Table("AIConversationLogs")]
    public class AIConversationLog
    {
        [Key]
        [Column("log_id")]
        public int LogId { get; set; }

        [Column("session_id")]
        public Guid SessionId { get; set; }

        [Required]
        [Column("user_message")]
        public string UserMessage { get; set; } = string.Empty;

        [Required]
        [Column("ai_response")]
        public string AIResponse { get; set; } = string.Empty;

        [StringLength(50)]
        [Column("intent_detected")]
        public string? IntentDetected { get; set; }

        [Column("confidence_score")]
        public decimal? ConfidenceScore { get; set; }

        [Column("was_escalated")]
        public bool WasEscalated { get; set; } = false;

        [Column("user_rating")]
        public int? UserRating { get; set; } // 1-5 stars

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

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
        public string Type { get; set; } = "general"; // chat_escalated, order_new, order_status, stock_alert

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

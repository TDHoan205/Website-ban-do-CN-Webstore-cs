using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webstore.Models.AI
{
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
        public int? UserRating { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

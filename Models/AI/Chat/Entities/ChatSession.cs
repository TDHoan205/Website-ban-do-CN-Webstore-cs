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
        public string Status { get; set; } = "active";

        [Column("assigned_to")]
        public int? AssignedTo { get; set; }

        [Column("started_at")]
        public DateTime StartedAt { get; set; } = DateTime.Now;

        [Column("ended_at")]
        public DateTime? EndedAt { get; set; }

        [ForeignKey("AccountId")]
        public virtual Account? Account { get; set; }
    }
}

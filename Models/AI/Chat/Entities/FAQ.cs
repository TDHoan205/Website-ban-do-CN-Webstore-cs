using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webstore.Models.AI
{
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
        public string Category { get; set; } = "general";

        [Column("keywords")]
        public string? Keywords { get; set; }

        [Column("priority")]
        public int Priority { get; set; } = 5;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

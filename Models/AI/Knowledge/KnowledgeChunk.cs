using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webstore.Models.AI
{
    /// <summary>
    /// Persisted vectorized chunk for RAG retrieval.
    /// </summary>
    [Table("KnowledgeChunks")]
    public class KnowledgeChunk
    {
        [Key]
        [Column("chunk_id")]
        public int ChunkId { get; set; }

        [Required]
        [StringLength(20)]
        [Column("source_type")]
        public string SourceType { get; set; } = string.Empty; // product | faq

        [Column("source_id")]
        public int SourceId { get; set; }

        [Required]
        [StringLength(30)]
        [Column("chunk_type")]
        public string ChunkType { get; set; } = string.Empty;

        [Required]
        [Column("raw_text")]
        public string RawText { get; set; } = string.Empty;

        [Required]
        [Column("normalized_text")]
        public string NormalizedText { get; set; } = string.Empty;

        [Required]
        [Column("embedding")]
        public string Embedding { get; set; } = string.Empty; // JSON float[]

        [Column("price", TypeName = "decimal(10,2)")]
        public decimal? Price { get; set; }

        [StringLength(100)]
        [Column("category")]
        public string? Category { get; set; }

        [Column("priority")]
        public int Priority { get; set; } = 0;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

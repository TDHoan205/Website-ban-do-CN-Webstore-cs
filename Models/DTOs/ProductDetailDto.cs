using System.Collections.Generic;

namespace Webstore.Models.DTOs
{
    /// <summary>
    /// DTO trả về cho trang chi tiết sản phẩm.
    /// Struct: Product + Variants + Images group theo VariantId (Shopee-style).
    /// </summary>
    public class ProductDetailDto
    {
        public Product Product { get; set; } = null!;
        public List<ProductVariant> Variants { get; set; } = new();

        /// <summary>
        /// Key = "variantId" (string) hoặc "default" cho ảnh product-level.
        /// Value = danh sách ProductImage đã được deduplicate.
        /// </summary>
        public Dictionary<string, List<ProductImage>> ImagesByVariant { get; set; } = new();

        /// <summary>
        /// Danh sách tất cả ảnh (flatten, deduplicate) — dùng làm fallback cuối cùng.
        /// </summary>
        public List<ProductImage> AllImages { get; set; } = new();
    }

    /// <summary>
    /// Ảnh trả về cho API / client.
    /// </summary>
    public class ProductImageDto
    {
        public int ImageId { get; set; }
        public int? VariantId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public bool IsThumbnail { get; set; }
        public int DisplayOrder { get; set; }
        public string? AltText { get; set; }
    }

    /// <summary>
    /// DTO gửi lên khi tạo/cập nhật ảnh từ admin.
    /// </summary>
    public class UpsertProductImageDto
    {
        public int? ImageId { get; set; }
        public int? VariantId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public bool IsThumbnail { get; set; }
        public int DisplayOrder { get; set; }
        public string? AltText { get; set; }
    }

    /// <summary>
    /// Request upload ảnh từ admin.
    /// </summary>
    public class UploadVariantImagesRequest
    {
        public int ProductId { get; set; }
        public int? VariantId { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsThumbnail { get; set; }
        public int DisplayOrder { get; set; }
        public string? AltText { get; set; }
    }
}

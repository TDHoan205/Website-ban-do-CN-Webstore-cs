using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Webstore.Models;
using Webstore.Utilities;
using Webstore.Services;
using Webstore.Data;

namespace Webstore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
        private readonly ISupplierService _supplierService;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly ApplicationDbContext _context;

        public ProductsController(
            IProductService productService,
            ISupplierService supplierService,
            IWebHostEnvironment hostEnvironment,
            ApplicationDbContext context)
        {
            _productService = productService;
            _supplierService = supplierService;
            _hostEnvironment = hostEnvironment;
            _context = context;
        }

        // GET: /Products
        public async Task<IActionResult> Index(string? search, string? sortOrder, int pageNumber = 1, int pageSize = 10)
        {
            var categoryId = (int?)null;
            var pList = await _productService.GetProductsAsync(search, categoryId, sortOrder, pageNumber, pageSize);

            ViewBag.NameSortParm = sortOrder == "name" ? "name_desc" : "name";
            ViewBag.PriceSortParm = sortOrder == "price" ? "price_desc" : "price";
            ViewBag.CategorySortParm = sortOrder == "category" ? "category_desc" : "category";
            ViewBag.SupplierSortParm = sortOrder == "supplier" ? "supplier_desc" : "supplier";

            ViewBag.Search = search;
            ViewBag.SortOrder = sortOrder;
            ViewBag.PageSize = pageSize;

            return View(pList);
        }

        private async Task LoadLookups()
        {
            ViewBag.Categories = await _productService.GetAllCategoriesAsync();
            ViewBag.Suppliers = await _supplierService.GetAllAsync();
        }

        // GET: /Products/Create
        public async Task<IActionResult> Create()
        {
            await LoadLookups();
            return View();
        }

        // POST: /Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Price,StockQuantity,IsAvailable,IsNew,IsHot,DiscountPercent,CategoryId,SupplierId")] Product product,
            IFormFile? imageFile,
            List<ProductVariant>? Variants,
            List<IFormFile>? NewProductLevelImages,
            Dictionary<string, List<string>>? NewVariantImages,
            int[]? ImagesToDelete)
        {
            if (!ModelState.IsValid)
            {
                await LoadLookups();
                return View(product);
            }

            product.Name = ProductDescriptionText.NormalizePlainText(product.Name) ?? string.Empty;
            product.Description = ProductDescriptionText.SanitizeDescriptionHtmlNullable(product.Description);

            if (imageFile != null && imageFile.Length > 0)
            {
                product.ImageUrl = await SaveImage(imageFile);
            }

            await _productService.CreateProductAsync(product);

            // Build index map: variant index -> variantId (after save, EF assigns IDs)
            var newVariantIndexToId = new Dictionary<int, int>();

            // Save variants first (get their IDs for image association)
            if (Variants != null && Variants.Count > 0)
            {
                int idx = 0;
                foreach (var variant in Variants)
                {
                    if (string.IsNullOrWhiteSpace(variant.Color) &&
                        string.IsNullOrWhiteSpace(variant.Storage) &&
                        string.IsNullOrWhiteSpace(variant.RAM))
                        { idx++; continue; }

                    variant.ProductId = product.ProductId;
                    if (variant.Price == 0 && product.Price > 0)
                        variant.Price = product.Price;

                    _context.ProductVariants.Add(variant);
                    idx++;
                }
                await _context.SaveChangesAsync();

                // Re-fetch to get assigned IDs in order
                var savedVariants = await _context.ProductVariants
                    .Where(v => v.ProductId == product.ProductId)
                    .OrderBy(v => v.VariantId)
                    .ToListAsync();
                for (int i = 0; i < savedVariants.Count; i++)
                    newVariantIndexToId[i] = savedVariants[i].VariantId;
            }

            // Save product-level images (IFormFile — actual file upload)
            if (NewProductLevelImages != null && NewProductLevelImages.Count > 0)
            {
                int displayOrder = 0;
                foreach (var imgFile in NewProductLevelImages.Where(f => f != null && f.Length > 0))
                {
                    var url = await SaveImage(imgFile);
                    _context.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.ProductId,
                        VariantId = null,
                        ImageUrl = url,
                        IsPrimary = displayOrder == 0,
                        IsThumbnail = displayOrder == 0,
                        DisplayOrder = displayOrder++
                    });
                }
                await _context.SaveChangesAsync();
            }

            // Save variant images
            // NewVariantImages keys: "variantId" (existing) or "new_X" (new variant by index)
            if (NewVariantImages != null && NewVariantImages.Count > 0)
            {
                foreach (var kvp in NewVariantImages)
                {
                    var key = kvp.Key;
                    var dataUrls = kvp.Value;

                    int? targetVariantId = null;

                    if (key.StartsWith("new_") && int.TryParse(key.Substring(4), out var idxFromKey))
                    {
                        // New variant — use the map built after variant save
                        if (newVariantIndexToId.TryGetValue(idxFromKey, out var assignedId))
                            targetVariantId = assignedId;
                    }
                    else if (int.TryParse(key, out var variantId))
                    {
                        // Existing variant
                        targetVariantId = variantId;
                    }

                    if (targetVariantId == null) continue;

                    int displayOrder = 0;
                    foreach (var dataUrl in dataUrls.Where(u => !string.IsNullOrEmpty(u)))
                    {
                        var url = await SaveBase64Image(dataUrl);
                        _context.ProductImages.Add(new ProductImage
                        {
                            ProductId = product.ProductId,
                            VariantId = targetVariantId.Value,
                            ImageUrl = url,
                            IsPrimary = displayOrder == 0,
                            IsThumbnail = displayOrder == 0,
                            DisplayOrder = displayOrder++
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }

            // Delete marked images
            if (ImagesToDelete != null && ImagesToDelete.Length > 0)
            {
                await DeleteImagesAsync(ImagesToDelete);
            }

            _productService.InvalidateCache();
            TempData["Success"] = "Tạo sản phẩm thành công";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var product = await _productService.GetProductByIdAsync(id.Value);
            if (product == null) return NotFound();
            await LoadLookups();

            var variants = await _context.ProductVariants
                .Where(v => v.ProductId == id)
                .OrderBy(v => v.DisplayOrder)
                .ToListAsync();
            ViewBag.Variants = variants;
            ViewBag.ProductId = product.ProductId;
            ViewBag.ProductImages = product.ProductImages?.ToList() ?? new List<ProductImage>();

            return View(product);
        }

        // POST: /Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,Name,Description,Price,StockQuantity,IsAvailable,IsNew,IsHot,DiscountPercent,CategoryId,SupplierId,ImageUrl")] Product product,
            IFormFile? imageFile,
            List<ProductVariant>? Variants,
            int[]? VariantsToDelete,
            List<IFormFile>? NewProductLevelImages,
            Dictionary<string, List<string>>? NewVariantImages,
            int[]? ImagesToDelete)
        {
            if (id != product.ProductId) return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadLookups();
                var existingVariants = await _context.ProductVariants.Where(v => v.ProductId == id).OrderBy(v => v.DisplayOrder).ToListAsync();
                ViewBag.Variants = existingVariants;
                return View(product);
            }

            try
            {
                product.Name = ProductDescriptionText.NormalizePlainText(product.Name) ?? string.Empty;
                product.Description = ProductDescriptionText.SanitizeDescriptionHtmlNullable(product.Description);

                if (imageFile != null && imageFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(product.ImageUrl)) DeleteImage(product.ImageUrl);
                    product.ImageUrl = await SaveImage(imageFile);
                }

                _context.Entry(product).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                // Delete marked variants (cascade delete their images)
                if (VariantsToDelete != null && VariantsToDelete.Length > 0)
                {
                    var toDelete = await _context.ProductVariants.Where(v => VariantsToDelete.Contains(v.VariantId)).ToListAsync();
                    // Delete images of deleted variants
                    var variantImages = await _context.ProductImages
                        .Where(pi => pi.VariantId != null && VariantsToDelete.Contains(pi.VariantId.Value))
                        .ToListAsync();
                    foreach (var img in variantImages) DeleteImage(img.ImageUrl);
                    _context.ProductImages.RemoveRange(variantImages);
                    _context.ProductVariants.RemoveRange(toDelete);
                }

                // Build map: index -> assigned VariantId for new variants
                var newVariantIndexToId = new Dictionary<int, int>();

                // Upsert variants
                if (Variants != null)
                {
                    int idx = 0;
                    foreach (var variant in Variants)
                    {
                        if (string.IsNullOrWhiteSpace(variant.Color) &&
                            string.IsNullOrWhiteSpace(variant.Storage) &&
                            string.IsNullOrWhiteSpace(variant.RAM))
                            { idx++; continue; }

                        variant.ProductId = product.ProductId;

                        if (variant.VariantId == 0)
                        {
                            // NEW variant
                            if (variant.Price == 0 && product.Price > 0)
                                variant.Price = product.Price;
                            _context.ProductVariants.Add(variant);
                            newVariantIndexToId[idx] = -1; // placeholder
                        }
                        else
                        {
                            // EXISTING variant
                            var existing = await _context.ProductVariants.FindAsync(variant.VariantId);
                            if (existing != null)
                            {
                                existing.Color = variant.Color;
                                existing.Storage = variant.Storage;
                                existing.RAM = variant.RAM;
                                existing.Price = variant.Price;
                                existing.StockQuantity = variant.StockQuantity;
                                existing.DisplayOrder = variant.DisplayOrder;
                                _context.Entry(existing).State = EntityState.Modified;
                            }
                        }
                        idx++;
                    }
                    await _context.SaveChangesAsync();

                    // Fill in assigned IDs for new variants
                    var allSaved = await _context.ProductVariants
                        .Where(v => v.ProductId == product.ProductId)
                        .OrderBy(v => v.VariantId)
                        .ToListAsync();

                    int savedIdx = 0;
                    idx = 0;
                    foreach (var variant in Variants)
                    {
                        if (string.IsNullOrWhiteSpace(variant.Color) &&
                            string.IsNullOrWhiteSpace(variant.Storage) &&
                            string.IsNullOrWhiteSpace(variant.RAM))
                            { idx++; continue; }

                        if (variant.VariantId == 0 && savedIdx < allSaved.Count)
                        {
                            newVariantIndexToId[idx] = allSaved[savedIdx].VariantId;
                            savedIdx++;
                        }
                        idx++;
                    }
                }

                // Save new product-level images (IFormFile — actual file upload)
                if (NewProductLevelImages != null && NewProductLevelImages.Count > 0)
                {
                    var existingCount = await _context.ProductImages
                        .CountAsync(pi => pi.ProductId == product.ProductId && pi.VariantId == null);
                    int displayOrder = existingCount;
                    foreach (var imgFile in NewProductLevelImages.Where(f => f != null && f.Length > 0))
                    {
                        var url = await SaveImage(imgFile);
                        _context.ProductImages.Add(new ProductImage
                        {
                            ProductId = product.ProductId,
                            VariantId = null,
                            ImageUrl = url,
                            IsPrimary = displayOrder == 0,
                            IsThumbnail = displayOrder == 0,
                            DisplayOrder = displayOrder++
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // Save variant images
                if (NewVariantImages != null && NewVariantImages.Count > 0)
                {
                    foreach (var kvp in NewVariantImages)
                    {
                        var key = kvp.Key;
                        var dataUrls = kvp.Value;

                        int? targetVariantId = null;

                        if (key.StartsWith("new_") && int.TryParse(key.Substring(4), out var idxFromKey))
                        {
                            if (newVariantIndexToId.TryGetValue(idxFromKey, out var assignedId) && assignedId > 0)
                                targetVariantId = assignedId;
                        }
                        else if (int.TryParse(key, out var variantId))
                        {
                            targetVariantId = variantId;
                        }

                        if (targetVariantId == null) continue;

                        int displayOrder = 0;
                        foreach (var dataUrl in dataUrls.Where(u => !string.IsNullOrEmpty(u)))
                        {
                            var url = await SaveBase64Image(dataUrl);
                            _context.ProductImages.Add(new ProductImage
                            {
                                ProductId = product.ProductId,
                                VariantId = targetVariantId.Value,
                                ImageUrl = url,
                                IsPrimary = displayOrder == 0,
                                IsThumbnail = displayOrder == 0,
                                DisplayOrder = displayOrder++
                            });
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                // Delete marked images
                if (ImagesToDelete != null && ImagesToDelete.Length > 0)
                {
                    await DeleteImagesAsync(ImagesToDelete);
                }

                _productService.InvalidateCache();
                TempData["Success"] = "Cập nhật sản phẩm thành công";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProductsController.Edit] Error: {ex}");
                TempData["Error"] = "Lỗi khi cập nhật: " + ex.Message;
                await LoadLookups();
                var existingVariants = await _context.ProductVariants.Where(v => v.ProductId == id).OrderBy(v => v.DisplayOrder).ToListAsync();
                ViewBag.Variants = existingVariants;
                return View(product);
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var product = await _productService.GetProductByIdAsync(id.Value);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: /Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product != null)
            {
                if (!string.IsNullOrEmpty(product.ImageUrl)) DeleteImage(product.ImageUrl);
                await _productService.DeleteProductAsync(id);
                TempData["Success"] = "Xóa sản phẩm thành công";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Products/DeleteProductImage/{imageId} — Xóa 1 ảnh (gọi từ JS trong admin)
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteProductImage(int imageId)
        {
            var img = await _context.ProductImages.FindAsync(imageId);
            if (img == null) return Json(new { success = false, message = "Ảnh không tồn tại." });

            DeleteImage(img.ImageUrl);
            _context.ProductImages.Remove(img);
            await _context.SaveChangesAsync();
            _productService.InvalidateCache();

            return Json(new { success = true, message = "Đã xóa ảnh." });
        }

        private async Task DeleteImagesAsync(IEnumerable<int> imageIds)
        {
            var images = await _context.ProductImages
                .Where(pi => imageIds.Contains(pi.ImageId))
                .ToListAsync();

            foreach (var img in images)
            {
                DeleteImage(img.ImageUrl);
                _context.ProductImages.Remove(img);
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>Save a file upload (IFormFile) to disk and return URL</summary>
        private async Task<string> SaveImage(IFormFile imageFile)
        {
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
            string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "products");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }
            return "/images/products/" + uniqueFileName;
        }

        /// <summary>Save a base64 data URL to disk and return URL (for JS-uploaded images)</summary>
        private async Task<string> SaveBase64Image(string dataUrl)
        {
            if (string.IsNullOrEmpty(dataUrl)) return string.Empty;

            // dataUrl format: "data:image/png;base64,xxxxx"
            var parts = dataUrl.Split(',');
            if (parts.Length < 2) return string.Empty;

            var header = parts[0]; // e.g. "data:image/png;base64"
            var base64Data = parts[1];

            // Determine extension from header
            string ext = ".png";
            if (header.Contains("jpeg") || header.Contains("jpg")) ext = ".jpg";
            else if (header.Contains("gif")) ext = ".gif";
            else if (header.Contains("webp")) ext = ".webp";

            string uniqueFileName = Guid.NewGuid().ToString() + ext;
            string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "products");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
            await System.IO.File.WriteAllBytesAsync(filePath, Convert.FromBase64String(base64Data));
            return "/images/products/" + uniqueFileName;
        }

        private void DeleteImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;
            string filePath = Path.Combine(_hostEnvironment.WebRootPath, imageUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
        }
    }
}

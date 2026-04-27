using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Webstore.Models;
using Webstore.Utilities;
using Webstore.Services;
using Webstore.Data;

namespace Webstore.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
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
        public async Task<IActionResult> Create([Bind("Name,Description,Price,StockQuantity,IsAvailable,IsNew,IsHot,DiscountPercent,CategoryId,SupplierId")] Product product, IFormFile? imageFile, List<ProductVariant>? Variants)
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

            // Save variants
            if (Variants != null && Variants.Count > 0)
            {
                foreach (var variant in Variants)
                {
                    if (string.IsNullOrWhiteSpace(variant.Color) &&
                        string.IsNullOrWhiteSpace(variant.Storage) &&
                        string.IsNullOrWhiteSpace(variant.RAM))
                        continue;

                    variant.ProductId = product.ProductId;
                    if (variant.Price == 0 && product.Price > 0)
                        variant.Price = product.Price;

                    _context.ProductVariants.Add(variant);
                }
                await _context.SaveChangesAsync();
            }

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

            return View(product);
        }

        // POST: /Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,Name,Description,Price,StockQuantity,IsAvailable,IsNew,IsHot,DiscountPercent,CategoryId,SupplierId,ImageUrl")] Product product, IFormFile? imageFile, List<ProductVariant>? Variants, int[]? VariantsToDelete)
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

                // Delete marked variants
                if (VariantsToDelete != null && VariantsToDelete.Length > 0)
                {
                    var toDelete = await _context.ProductVariants.Where(v => VariantsToDelete.Contains(v.VariantId)).ToListAsync();
                    _context.ProductVariants.RemoveRange(toDelete);
                }

                // Upsert variants
                if (Variants != null)
                {
                    foreach (var variant in Variants)
                    {
                        if (string.IsNullOrWhiteSpace(variant.Color) &&
                            string.IsNullOrWhiteSpace(variant.Storage) &&
                            string.IsNullOrWhiteSpace(variant.RAM))
                            continue;

                        variant.ProductId = product.ProductId;

                        if (variant.VariantId == 0)
                        {
                            // New variant
                            if (variant.Price == 0 && product.Price > 0)
                                variant.Price = product.Price;
                            _context.ProductVariants.Add(variant);
                        }
                        else
                        {
                            // Existing variant - update
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
                    }
                }

                await _context.SaveChangesAsync();
                _productService.InvalidateCache();

                TempData["Success"] = "Cập nhật sản phẩm thành công";
            }
            catch (Exception ex)
            {
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

        private void DeleteImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;
            string filePath = Path.Combine(_hostEnvironment.WebRootPath, imageUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
        }
    }
}

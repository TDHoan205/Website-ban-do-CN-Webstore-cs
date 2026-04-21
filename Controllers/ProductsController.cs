using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Webstore.Models;
using Webstore.Utilities;
using Webstore.Services;

namespace Webstore.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
        private readonly ISupplierService _supplierService;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductsController(IProductService productService, ISupplierService supplierService, IWebHostEnvironment hostEnvironment)
        {
            _productService = productService;
            _supplierService = supplierService;
            _hostEnvironment = hostEnvironment;
        }

        // GET: /Products
        public async Task<IActionResult> Index(string? search, string? sortOrder, int pageNumber = 1, int pageSize = 10)
        {
            // Note: IProductService currently has GetProductsAsync but it returns a PaginatedList specialized for Shop.
            // For Admin, we might want a slightly different view, but for now let's reuse or use the repo directly if needed.
            // Actually, stay consistent: use the service.
            
            var categoryId = (int?)null; // No category filter by default in admin index unless specified
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
        public async Task<IActionResult> Create([Bind("Name,Description,Price,CategoryId,SupplierId")] Product product, IFormFile? imageFile)
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
            return View(product);
        }

        // POST: /Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,Name,Description,Price,CategoryId,SupplierId,ImageUrl")] Product product, IFormFile? imageFile)
        {
            if (id != product.ProductId) return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadLookups();
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

                await _productService.UpdateProductAsync(product);
                TempData["Success"] = "Cập nhật sản phẩm thành công";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi cập nhật: " + ex.Message;
                await LoadLookups();
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

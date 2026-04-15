using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Webstore.Data;
using Webstore.Models;
using Webstore.Utilities;

namespace Webstore.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: /Products
        public async Task<IActionResult> Index(string? search, string? sortOrder, int pageNumber = 1, int pageSize = 10)
        {
            var query = _context.Products.Include(p => p.Category).Include(p => p.Supplier).AsQueryable();
            
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Name.Contains(search)
                                       || (p.Description != null && p.Description.Contains(search))
                                       || (p.Category != null && p.Category.Name.Contains(search))
                                       || (p.Supplier != null && p.Supplier.Name.Contains(search)));
            }

            // Sorting
            ViewBag.NameSortParm = sortOrder == "name" ? "name_desc" : "name";
            ViewBag.PriceSortParm = sortOrder == "price" ? "price_desc" : "price";
            ViewBag.CategorySortParm = sortOrder == "category" ? "category_desc" : "category";
            ViewBag.SupplierSortParm = sortOrder == "supplier" ? "supplier_desc" : "supplier";

            query = sortOrder switch
            {
                "name_desc" => query.OrderByDescending(p => p.Name),
                "price" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "category" => query.OrderBy(p => p.Category != null ? p.Category.Name : ""),
                "category_desc" => query.OrderByDescending(p => p.Category != null ? p.Category.Name : ""),
                "supplier" => query.OrderBy(p => p.Supplier != null ? p.Supplier.Name : ""),
                "supplier_desc" => query.OrderByDescending(p => p.Supplier != null ? p.Supplier.Name : ""),
                _ => query.OrderBy(p => p.Name)
            };

            var products = await PagedList<Product>.CreateAsync(query, pageNumber, pageSize);
            
            ViewBag.Search = search;
            ViewBag.SortOrder = sortOrder;
            ViewBag.PageSize = pageSize;
            
            return View(products);
        }

        private async Task LoadLookups()
        {
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Suppliers = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();
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
            await LoadLookups();

            product.Name = ProductDescriptionText.NormalizePlainText(product.Name) ?? string.Empty;
            product.Description = ProductDescriptionText.SanitizeDescriptionHtmlNullable(product.Description);

            if (!ModelState.IsValid)
            {
                return View(product);
            }

            // Xử lý upload ảnh
            if (imageFile != null && imageFile.Length > 0)
            {
                product.ImageUrl = await SaveImage(imageFile);
            }

            _context.Add(product);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tạo sản phẩm thành công";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.FindAsync(id);
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
            await LoadLookups();

            product.Name = ProductDescriptionText.NormalizePlainText(product.Name) ?? string.Empty;
            product.Description = ProductDescriptionText.SanitizeDescriptionHtmlNullable(product.Description);

            if (!ModelState.IsValid)
            {
                return View(product);
            }

            try
            {
                // Xử lý upload ảnh mới
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Xóa ảnh cũ nếu có
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        DeleteImage(product.ImageUrl);
                    }
                    product.ImageUrl = await SaveImage(imageFile);
                }

                _context.Update(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật sản phẩm thành công";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Products.AnyAsync(e => e.ProductId == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.Include(p => p.Category).Include(p => p.Supplier)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: /Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            // Xóa ảnh nếu có
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                DeleteImage(product.ImageUrl);
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa sản phẩm thành công";
            return RedirectToAction(nameof(Index));
        }

        // Helper methods cho upload ảnh
        private async Task<string> SaveImage(IFormFile imageFile)
        {
            // Tạo tên file unique
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
            string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "products");
            
            // Tạo thư mục nếu chưa tồn tại
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            // Trả về đường dẫn tương đối để lưu vào database
            return "/images/products/" + uniqueFileName;
        }

        private void DeleteImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            string filePath = Path.Combine(_hostEnvironment.WebRootPath, imageUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
    }
}


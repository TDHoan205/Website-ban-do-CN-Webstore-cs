using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Webstore.Models;
using Webstore.Services;

namespace Webstore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // GET: /Categories
        public async Task<IActionResult> Index(string? search, string? sortOrder, int pageNumber = 1, int pageSize = 10)
        {
            var allCategories = await _categoryService.GetAllAsync();
            var query = allCategories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            // Sorting
            ViewBag.NameSortParm = sortOrder == "name" ? "name_desc" : "name";

            query = sortOrder switch
            {
                "name_desc" => query.OrderByDescending(c => c.Name),
                _ => query.OrderBy(c => c.Name)
            };

            var totalItems = query.Count();
            var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            var pagedList = new PagedList<Category>(items, totalItems, pageNumber, pageSize);
            
            ViewBag.Search = search;
            ViewBag.SortOrder = sortOrder;
            ViewBag.PageSize = pageSize;
            
            return View(pagedList);
        }

        // GET: /Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Category category)
        {
            if (!ModelState.IsValid)
            {
                return View(category);
            }

            // Unique Name validation
            var all = await _categoryService.GetAllAsync();
            if (all.Any(c => c.Name == category.Name))
            {
                ModelState.AddModelError("Name", "Tên danh mục đã tồn tại.");
                return View(category);
            }

            await _categoryService.CreateAsync(category);
            TempData["Success"] = "Tạo danh mục thành công";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var category = await _categoryService.GetByIdAsync(id.Value);
            if (category == null) return NotFound();

            return View(category);
        }

        // POST: /Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryId,Name")] Category category)
        {
            if (id != category.CategoryId) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            var all = await _categoryService.GetAllAsync();
            if (all.Any(c => c.Name == category.Name && c.CategoryId != category.CategoryId))
            {
                ModelState.AddModelError("Name", "Tên danh mục đã tồn tại.");
                return View(category);
            }

            try
            {
                await _categoryService.UpdateAsync(category);
                TempData["Success"] = "Cập nhật danh mục thành công";
            }
            catch (Exception)
            {
                if (await _categoryService.GetByIdAsync(id) == null) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var category = await _categoryService.GetByIdAsync(id.Value);
            if (category == null) return NotFound();

            return View(category);
        }

        // POST: /Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _categoryService.DeleteAsync(id);
            TempData["Success"] = "Xóa danh mục thành công";
            return RedirectToAction(nameof(Index));
        }
    }
}

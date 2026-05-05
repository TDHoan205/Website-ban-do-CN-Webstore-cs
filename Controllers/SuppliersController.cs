using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Webstore.Models;
using Webstore.Services;

namespace Webstore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SuppliersController : Controller
    {
        private readonly ISupplierService _supplierService;

        public SuppliersController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        // GET: /Suppliers
        public async Task<IActionResult> Index(string? search, string? sortOrder, int pageNumber = 1, int pageSize = 10)
        {
            var allSuppliers = await _supplierService.GetAllAsync();
            var query = allSuppliers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s => s.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            // Sorting
            ViewBag.NameSortParm = sortOrder == "name" ? "name_desc" : "name";
            ViewBag.ContactSortParm = sortOrder == "contact" ? "contact_desc" : "contact";

            query = sortOrder switch
            {
                "name_desc" => query.OrderByDescending(s => s.Name),
                "contact" => query.OrderBy(s => s.ContactPerson),
                "contact_desc" => query.OrderByDescending(s => s.ContactPerson),
                _ => query.OrderBy(s => s.Name)
            };

            var totalItems = query.Count();
            var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            var pagedList = new PagedList<Supplier>(items, totalItems, pageNumber, pageSize);
            
            ViewBag.Search = search;
            ViewBag.SortOrder = sortOrder;
            ViewBag.PageSize = pageSize;
            
            return View(pagedList);
        }

        // GET: /Suppliers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Suppliers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,ContactPerson,Phone,Email,Address")] Supplier supplier)
        {
            if (!ModelState.IsValid)
            {
                return View(supplier);
            }

            await _supplierService.CreateAsync(supplier);
            TempData["Success"] = "Tạo nhà cung cấp thành công";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Suppliers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var supplier = await _supplierService.GetByIdAsync(id.Value);
            if (supplier == null) return NotFound();

            return View(supplier);
        }

        // POST: /Suppliers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("SupplierId,Name,ContactPerson,Phone,Email,Address")] Supplier supplier)
        {
            if (id != supplier.SupplierId) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(supplier);
            }

            try
            {
                await _supplierService.UpdateAsync(supplier);
                TempData["Success"] = "Cập nhật nhà cung cấp thành công";
            }
            catch (Exception)
            {
                if (await _supplierService.GetByIdAsync(id) == null) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Suppliers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var supplier = await _supplierService.GetByIdAsync(id.Value);
            if (supplier == null) return NotFound();

            return View(supplier);
        }

        // POST: /Suppliers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _supplierService.DeleteAsync(id);
            TempData["Success"] = "Xóa nhà cung cấp thành công";
            return RedirectToAction(nameof(Index));
        }
    }
}

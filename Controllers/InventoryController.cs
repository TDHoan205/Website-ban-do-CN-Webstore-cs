using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Webstore.Data;
using Webstore.Models;

namespace Webstore.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Inventory
        public async Task<IActionResult> Index(string? search, string? sortOrder, int pageNumber = 1, int pageSize = 10)
        {
            var query = _context.Inventory.Include(i => i.Product).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(i => i.Product != null && (
                    (i.Product.Name != null && i.Product.Name.ToLower().Contains(searchLower))
                    || (i.Product.Description != null && i.Product.Description.ToLower().Contains(searchLower))));
            }

            // Sorting
            ViewBag.ProductSortParm = sortOrder == "product" ? "product_desc" : "product";
            ViewBag.QuantitySortParm = sortOrder == "quantity" ? "quantity_desc" : "quantity";
            ViewBag.DateSortParm = sortOrder == "date" ? "date_desc" : "date";

            query = sortOrder switch
            {
                "product_desc" => query.OrderByDescending(i => i.Product != null ? i.Product.Name ?? "" : ""),
                "quantity" => query.OrderBy(i => i.QuantityInStock),
                "quantity_desc" => query.OrderByDescending(i => i.QuantityInStock),
                "date" => query.OrderBy(i => i.LastUpdatedDate),
                "date_desc" => query.OrderByDescending(i => i.LastUpdatedDate),
                _ => query.OrderBy(i => i.Product != null ? i.Product.Name ?? "" : "")
            };

            var inventories = await PagedList<Inventory>.CreateAsync(query, pageNumber, pageSize);


            ViewBag.Search = search;
            ViewBag.SortOrder = sortOrder;
            ViewBag.PageSize = pageSize;

            return View(inventories);
        }

        // GET: /Inventory/Create
        public async Task<IActionResult> Create()
        {
            var products = await _context.Products
                .Where(p => !_context.Inventory.Any(i => i.ProductId == p.ProductId))
                .OrderBy(p => p.Name)
                .ToListAsync();
            ViewBag.Products = products;
            return View();
        }

        // POST: /Inventory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,QuantityInStock")] Inventory inventory)
        {
            var products = await _context.Products
                .Where(p => !_context.Inventory.Any(i => i.ProductId == p.ProductId))
                .OrderBy(p => p.Name)
                .ToListAsync();
            ViewBag.Products = products;

            if (_context.Inventory.Any(i => i.ProductId == inventory.ProductId))
            {
                ModelState.AddModelError("ProductId", "Sản phẩm này đã có tồn kho.");
            }

            if (!ModelState.IsValid)
            {
                // Put ModelState errors into TempData to make debugging easier in UI
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
                if (errors.Length > 0)
                {
                    TempData["Error"] = string.Join("; ", errors);
                }
                return View(inventory);
            }

            inventory.LastUpdatedDate = DateTime.Now;
            _context.Add(inventory);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tạo tồn kho thành công";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Inventory/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var inventory = await _context.Inventory.Include(i => i.Product).FirstOrDefaultAsync(i => i.InventoryId == id);
            if (inventory == null) return NotFound();

            return View(inventory);
        }

        // POST: /Inventory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("InventoryId,ProductId,QuantityInStock")] Inventory inventory)
        {
            if (id != inventory.InventoryId) return NotFound();
            if (!ModelState.IsValid)
            {
                var inventoryWithProduct = await _context.Inventory.Include(i => i.Product).FirstOrDefaultAsync(i => i.InventoryId == id);
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
                if (errors.Length > 0)
                {
                    TempData["Error"] = string.Join("; ", errors);
                }
                return View(inventoryWithProduct);
            }

            try
            {
                var dbInventory = await _context.Inventory.FirstOrDefaultAsync(i => i.InventoryId == id);
                if (dbInventory == null) return NotFound();

                // Update only allowed fields to avoid accidental overwrites
                dbInventory.QuantityInStock = inventory.QuantityInStock;
                dbInventory.LastUpdatedDate = DateTime.Now;

                _context.Update(dbInventory);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật tồn kho thành công";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Inventory.AnyAsync(e => e.InventoryId == id))
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

        // GET: /Inventory/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var inventory = await _context.Inventory.Include(i => i.Product).FirstOrDefaultAsync(m => m.InventoryId == id);
            if (inventory == null) return NotFound();

            return View(inventory);
        }

        // POST: /Inventory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inventory = await _context.Inventory.FindAsync(id);
            if (inventory == null) return NotFound();

            _context.Inventory.Remove(inventory);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa tồn kho thành công";
            return RedirectToAction(nameof(Index));
        }
    }
}

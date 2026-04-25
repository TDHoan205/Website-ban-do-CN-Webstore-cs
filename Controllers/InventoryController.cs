using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Webstore.Models;
using Webstore.Services;

namespace Webstore.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventoryService;
        private readonly IProductService _productService;

        public InventoryController(IInventoryService inventoryService, IProductService productService)
        {
            _inventoryService = inventoryService;
            _productService = productService;
        }

        // GET: /Inventory
        public async Task<IActionResult> Index(string? search, string? sortOrder, int pageNumber = 1, int pageSize = 10)
        {
            var allInventory = await _inventoryService.GetAllAsync();
            var query = allInventory.AsQueryable();

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
                "product_desc" => query.OrderByDescending(i => i.Product != null ? i.Product.Name : ""),
                "quantity" => query.OrderBy(i => i.StockQuantity),
                "quantity_desc" => query.OrderByDescending(i => i.StockQuantity),
                "date" => query.OrderBy(i => i.LastUpdated),
                "date_desc" => query.OrderByDescending(i => i.LastUpdated),
                _ => query.OrderBy(i => i.Product != null ? i.Product.Name : "")
            };

            var totalItems = query.Count();
            var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            var paginatedList = new PagedList<Inventory>(items, totalItems, pageNumber, pageSize);

            ViewBag.Search = search;
            ViewBag.SortOrder = sortOrder;
            ViewBag.PageSize = pageSize;

            return View(paginatedList);
        }

        // GET: /Inventory/Create
        public async Task<IActionResult> Create()
        {
            var pagedProducts = await _productService.GetProductsAsync(null, null, null, 1, 1000);
            var allInventory = await _inventoryService.GetAllAsync();
            var productsWithoutInventory = pagedProducts
                .Where(p => !allInventory.Any(i => i.ProductId == p.ProductId))
                .OrderBy(p => p.Name)
                .ToList();
            
            ViewBag.Products = productsWithoutInventory;
            return View();
        }

        // POST: /Inventory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,StockQuantity")] Inventory inventory)
        {
            if (await _inventoryService.GetByProductIdAsync(inventory.ProductId) != null)
            {
                ModelState.AddModelError("ProductId", "Sản phẩm này đã có tồn kho.");
            }

            if (!ModelState.IsValid)
            {
                var pagedProducts = await _productService.GetProductsAsync(null, null, null, 1, 1000);
                var allInventory = await _inventoryService.GetAllAsync();
                ViewBag.Products = pagedProducts
                    .Where(p => !allInventory.Any(i => i.ProductId == p.ProductId))
                    .OrderBy(p => p.Name)
                    .ToList();
                return View(inventory);
            }

            await _inventoryService.CreateAsync(inventory);
            TempData["Success"] = "Tạo tồn kho thành công";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Inventory/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var inventory = await _inventoryService.GetByIdAsync(id.Value);
            if (inventory == null) return NotFound();

            return View(inventory);
        }

        // POST: /Inventory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("InventoryId,ProductId,StockQuantity")] Inventory inventory)
        {
            if (id != inventory.InventoryId) return NotFound();
            
            if (!ModelState.IsValid)
            {
                var dbInventory = await _inventoryService.GetByIdAsync(id);
                return View(dbInventory);
            }

            try
            {
                await _inventoryService.UpdateAsync(inventory);
                TempData["Success"] = "Cập nhật tồn kho thành công";
            }
            catch (Exception)
            {
                if (await _inventoryService.GetByIdAsync(id) == null) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /Inventory/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var inventory = await _inventoryService.GetByIdAsync(id.Value);
            if (inventory == null) return NotFound();

            return View(inventory);
        }

        // POST: /Inventory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _inventoryService.DeleteAsync(id);
            TempData["Success"] = "Xóa tồn kho thành công";
            return RedirectToAction(nameof(Index));
        }
    }
}

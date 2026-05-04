using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Webstore.Data;
using Webstore.Models;

namespace Webstore.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class OrderItemsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /OrderItems
        public async Task<IActionResult> Index(string? search, string? sortOrder, int pageNumber = 1, int pageSize = 10)
        {
            var query = _context.OrderItems.Include(oi => oi.Order).Include(oi => oi.Product).Include(oi => oi.Variant).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(oi => oi.OrderItemId.ToString().Contains(search)
                                       || oi.OrderId.ToString().Contains(search)
                                       || (oi.Order != null && oi.Order.OrderId.ToString().Contains(search))
                                       || (oi.Product != null && oi.Product.Name.Contains(search)));
            }

            // Sorting
            ViewBag.OrderItemIdSortParm = sortOrder == "orderitemid" ? "orderitemid_desc" : "orderitemid";
            ViewBag.OrderSortParm = sortOrder == "order" ? "order_desc" : "order";
            ViewBag.ProductSortParm = sortOrder == "product" ? "product_desc" : "product";
            ViewBag.QuantitySortParm = sortOrder == "quantity" ? "quantity_desc" : "quantity";

            query = sortOrder switch
            {
                "orderitemid_desc" => query.OrderByDescending(oi => oi.OrderItemId),
                "order" => query.OrderBy(oi => oi.Order != null ? oi.Order.OrderId : 0),
                "order_desc" => query.OrderByDescending(oi => oi.Order != null ? oi.Order.OrderId : 0),
                "product" => query.OrderBy(oi => oi.Product != null ? oi.Product.Name : ""),
                "product_desc" => query.OrderByDescending(oi => oi.Product != null ? oi.Product.Name : ""),
                "quantity" => query.OrderBy(oi => oi.Quantity),
                "quantity_desc" => query.OrderByDescending(oi => oi.Quantity),
                _ => query.OrderBy(oi => oi.OrderItemId)
            };

            var orderItems = await PagedList<OrderItem>.CreateAsync(query, pageNumber, pageSize);

            ViewBag.Search = search;
            ViewBag.SortOrder = sortOrder;
            ViewBag.PageSize = pageSize;

            return View(orderItems);
        }

        private async Task LoadLookups()
        {
            ViewBag.Orders = await _context.Orders.OrderBy(o => o.OrderId).ToListAsync();
            ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
        }

        // GET: /OrderItems/Create
        public async Task<IActionResult> Create()
        {
            await LoadLookups();
            return View();
        }

        // POST: /OrderItems/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderId,ProductId,VariantId,Quantity,UnitPrice")] OrderItem orderItem)
        {
            await LoadLookups();
            if (!ModelState.IsValid)
            {
                return View(orderItem);
            }

            _context.Add(orderItem);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tạo chi tiết đơn hàng thành công";
            return RedirectToAction(nameof(Index));
        }

        // GET: /OrderItems/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var orderItem = await _context.OrderItems.FindAsync(id.Value);
            if (orderItem == null) return NotFound();
            await LoadLookups();
            return View(orderItem);
        }

        // POST: /OrderItems/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrderItemId,OrderId,ProductId,VariantId,Quantity,UnitPrice")] OrderItem orderItem)
        {
            if (id != orderItem.OrderItemId) return NotFound();
            await LoadLookups();
            if (!ModelState.IsValid)
            {
                return View(orderItem);
            }
            try
            {
                _context.Update(orderItem);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật chi tiết đơn hàng thành công";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.OrderItems.AnyAsync(e => e.OrderItemId == id))
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

        // GET: /OrderItems/Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var orderItem = await _context.OrderItems.Include(oi => oi.Order).Include(oi => oi.Product)
                .FirstOrDefaultAsync(m => m.OrderItemId == id.Value);
            if (orderItem == null) return NotFound();
            return View(orderItem);
        }

        // POST: /OrderItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var orderItem = await _context.OrderItems.FindAsync(id);
            if (orderItem == null) return NotFound();
            _context.OrderItems.Remove(orderItem);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa chi tiết đơn hàng thành công";
            return RedirectToAction(nameof(Index));
        }
    }
}

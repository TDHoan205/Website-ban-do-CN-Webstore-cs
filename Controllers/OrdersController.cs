using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Webstore.Data;
using Webstore.Models;

namespace Webstore.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Orders
        public async Task<IActionResult> Index(string? search, string? sortOrder, int pageNumber = 1, int pageSize = 10)
        {
            var query = _context.Orders.Include(o => o.Account).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(o => o.OrderId.ToString().Contains(search)
                                       || (o.Account != null && o.Account.Username.Contains(search))
                                       || o.Status.Contains(search));
            }

            // Sorting
            ViewBag.OrderIdSortParm = sortOrder == "orderid" ? "orderid_desc" : "orderid";
            ViewBag.DateSortParm = sortOrder == "date" ? "date_desc" : "date";
            ViewBag.AmountSortParm = sortOrder == "amount" ? "amount_desc" : "amount";
            ViewBag.StatusSortParm = sortOrder == "status" ? "status_desc" : "status";

            query = sortOrder switch
            {
                "orderid_desc" => query.OrderByDescending(o => o.OrderId),
                "date" => query.OrderBy(o => o.OrderDate),
                "date_desc" => query.OrderByDescending(o => o.OrderDate),
                "amount" => query.OrderBy(o => o.TotalAmount),
                "amount_desc" => query.OrderByDescending(o => o.TotalAmount),
                "status" => query.OrderBy(o => o.Status),
                "status_desc" => query.OrderByDescending(o => o.Status),
                _ => query.OrderByDescending(o => o.OrderDate)
            };

            var orders = await PagedList<Order>.CreateAsync(query, pageNumber, pageSize);

            ViewBag.Search = search;
            ViewBag.SortOrder = sortOrder;
            ViewBag.PageSize = pageSize;

            return View(orders);
        }

        private async Task LoadLookups()
        {
            ViewBag.Accounts = await _context.Accounts.OrderBy(a => a.Username).ToListAsync();
        }

        // GET: /Orders/Create
        public async Task<IActionResult> Create()
        {
            await LoadLookups();
            return View();
        }

        // GET: /Orders/DetailsPartial/5
        // Returns a partial view with order details (used for inline display in Index)
        public async Task<IActionResult> DetailsPartial(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.Account)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id.Value);

            if (order == null) return NotFound();

            return PartialView("_OrderDetailsPartial", order);
        }

        // POST: /Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AccountId,OrderDate,TotalAmount,Status")] Order order)
        {
            await LoadLookups();
            if (!ModelState.IsValid)
            {
                return View(order);
            }

            _context.Add(order);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tạo đơn hàng thành công";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();
            await LoadLookups();
            return View(order);
        }

        // POST: /Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrderId,AccountId,OrderDate,TotalAmount,Status")] Order order)
        {
            if (id != order.OrderId) return NotFound();
            await LoadLookups();
            if (!ModelState.IsValid)
            {
                return View(order);
            }
            try
            {
                _context.Update(order);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật đơn hàng thành công";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Orders.AnyAsync(e => e.OrderId == id))
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

        // GET: /Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var order = await _context.Orders.Include(o => o.Account)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null) return NotFound();
            return View(order);
        }

        // POST: /Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa đơn hàng thành công";
            return RedirectToAction(nameof(Index));
        }
    }
}

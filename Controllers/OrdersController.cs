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
        public async Task<IActionResult> Index(
            string? search,
            string? statusFilter,
            string? sortOrder,
            int pageNumber = 1,
            int pageSize = 15)
        {
            var query = _context.Orders
                .Include(o => o.Account)
                .Include(o => o.OrderItems)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(o => o.Status == statusFilter);
            }

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(o =>
                    o.OrderId.ToString().Contains(search) ||
                    (o.Account != null && o.Account.Username.Contains(search)) ||
                    (o.CustomerName != null && o.CustomerName.Contains(search)) ||
                    o.CustomerPhone.Contains(search));
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

            // Counts for badges
            ViewBag.Search = search;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.SortOrder = sortOrder;
            ViewBag.PageSize = pageSize;
            ViewBag.AllStatuses = OrderStatus.All;

            ViewBag.CountPending = await _context.Orders.CountAsync(o => o.Status == "Pending");
            ViewBag.CountAwaiting = await _context.Orders.CountAsync(o => o.Status == "AwaitingConfirmation");
            ViewBag.CountConfirmed = await _context.Orders.CountAsync(o => o.Status == "Confirmed");
            ViewBag.CountProcessing = await _context.Orders.CountAsync(o => o.Status == "Processing");
            ViewBag.CountShipped = await _context.Orders.CountAsync(o => o.Status == "Shipped");
            ViewBag.CountDelivered = await _context.Orders.CountAsync(o => o.Status == "Delivered");
            ViewBag.CountCancelled = await _context.Orders.CountAsync(o => o.Status == "Cancelled");

            return View(orders);
        }

        // GET: /Orders/PendingPayments - Đơn chờ xác nhận thanh toán
        public async Task<IActionResult> PendingPayments(string? sortOrder, int pageNumber = 1)
        {
            return RedirectToAction("Index", new { statusFilter = "AwaitingConfirmation", sortOrder, pageNumber });
        }

        // GET: /Orders/DetailsPartial/5
        public async Task<IActionResult> DetailsPartial(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.Account)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variant)
                .FirstOrDefaultAsync(o => o.OrderId == id.Value);

            if (order == null) return NotFound();
            return PartialView("_OrderDetailsPartial", order);
        }

        // GET: /Orders/Manage/5 - Trang quản lý chi tiết đơn hàng cho admin
        public async Task<IActionResult> Manage(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.Account)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variant)
                .FirstOrDefaultAsync(o => o.OrderId == id.Value);

            if (order == null) return NotFound();

            ViewBag.AllStatuses = OrderStatus.All;
            return View("Manage", order);
        }

        // POST: /Orders/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
            }

            if (!OrderStatus.All.Contains(status))
            {
                return Json(new { success = false, message = "Trạng thái không hợp lệ." });
            }

            order.Status = status;
            await _context.SaveChangesAsync();

            var displayName = OrderStatus.GetDisplayName(status);
            return Json(new { success = true, message = $"Cập nhật trạng thái thành: {displayName}", status, displayName });
        }

        // POST: /Orders/ConfirmPayment/5 - Admin xác nhận thanh toán
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index");
            }

            if (order.Status != "AwaitingConfirmation")
            {
                TempData["Error"] = $"Không thể xác nhận. Đơn hàng đang ở trạng thái: {OrderStatus.GetDisplayName(order.Status)}";
                return RedirectToAction("Manage", new { id = orderId });
            }

            order.Status = "Processing";
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đơn hàng #{orderId} đã được xác nhận thanh toán và đang chuẩn bị hàng.";

            return RedirectToAction("Manage", new { id = orderId });
        }

        // POST: /Orders/RejectPayment/5 - Admin từ chối thanh toán
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPayment(int orderId, string? reason)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index");
            }

            if (order.Status != "AwaitingConfirmation")
            {
                TempData["Error"] = "Chỉ có thể từ chối đơn ở trạng thái chờ xác nhận.";
                return RedirectToAction("Manage", new { id = orderId });
            }

            order.Status = "PaymentFailed";
            if (!string.IsNullOrWhiteSpace(reason))
            {
                order.Notes = string.IsNullOrWhiteSpace(order.Notes)
                    ? $"Lý do từ chối: {reason}"
                    : order.Notes + $" | Lý do từ chối: {reason}";
            }
            await _context.SaveChangesAsync();
            TempData["Warning"] = $"Đơn hàng #{orderId} đã bị đánh dấu thanh toán thất bại.";

            return RedirectToAction("Manage", new { id = orderId });
        }

        // GET: /Orders/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Accounts = await _context.Accounts.OrderBy(a => a.Username).ToListAsync();
            ViewBag.AllStatuses = OrderStatus.All;
            return View();
        }

        // POST: /Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AccountId,OrderDate,TotalAmount,Status")] Order order)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Accounts = await _context.Accounts.OrderBy(a => a.Username).ToListAsync();
                ViewBag.AllStatuses = OrderStatus.All;
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
            ViewBag.Accounts = await _context.Accounts.OrderBy(a => a.Username).ToListAsync();
            ViewBag.AllStatuses = OrderStatus.All;
            return View(order);
        }

        // POST: /Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrderId,AccountId,OrderDate,TotalAmount,Status")] Order order)
        {
            if (id != order.OrderId) return NotFound();
            if (!ModelState.IsValid)
            {
                ViewBag.Accounts = await _context.Accounts.OrderBy(a => a.Username).ToListAsync();
                ViewBag.AllStatuses = OrderStatus.All;
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
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var order = await _context.Orders.Include(o => o.Account).FirstOrDefaultAsync(m => m.OrderId == id);
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

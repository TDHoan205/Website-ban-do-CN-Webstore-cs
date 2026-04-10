using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Webstore.Data;
using Webstore.Models;

namespace Webstore.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class ReceiptShipmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReceiptShipmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /ReceiptShipments
        public async Task<IActionResult> Index(string? search, string? sortOrder, int pageNumber = 1, int pageSize = 10)
        {
            var query = _context.ReceiptShipments.Include(rs => rs.Product).Include(rs => rs.RelatedOrder).AsQueryable();
            
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(rs => rs.MovementId.ToString().Contains(search)
                                       || (rs.Product != null && rs.Product.Name.Contains(search))
                                       || rs.MovementType.Contains(search)
                                       || (rs.RelatedOrder != null && rs.RelatedOrder.OrderId.ToString().Contains(search)));
            }

            // Sorting
            ViewBag.MovementIdSortParm = sortOrder == "movementid" ? "movementid_desc" : "movementid";
            ViewBag.ProductSortParm = sortOrder == "product" ? "product_desc" : "product";
            ViewBag.TypeSortParm = sortOrder == "type" ? "type_desc" : "type";
            ViewBag.QuantitySortParm = sortOrder == "quantity" ? "quantity_desc" : "quantity";
            ViewBag.DateSortParm = sortOrder == "date" ? "date_desc" : "date";

            query = sortOrder switch
            {
                "movementid_desc" => query.OrderByDescending(rs => rs.MovementId),
                "product" => query.OrderBy(rs => rs.Product != null ? rs.Product.Name : ""),
                "product_desc" => query.OrderByDescending(rs => rs.Product != null ? rs.Product.Name : ""),
                "type" => query.OrderBy(rs => rs.MovementType),
                "type_desc" => query.OrderByDescending(rs => rs.MovementType),
                "quantity" => query.OrderBy(rs => rs.Quantity),
                "quantity_desc" => query.OrderByDescending(rs => rs.Quantity),
                "date" => query.OrderBy(rs => rs.MovementDate),
                "date_desc" => query.OrderByDescending(rs => rs.MovementDate),
                _ => query.OrderByDescending(rs => rs.MovementDate)
            };

            var receiptShipments = await PagedList<ReceiptShipment>.CreateAsync(query, pageNumber, pageSize);
            
            ViewBag.Search = search;
            ViewBag.SortOrder = sortOrder;
            ViewBag.PageSize = pageSize;
            
            return View(receiptShipments);
        }

        private async Task LoadLookups()
        {
            ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            ViewBag.Orders = await _context.Orders.OrderBy(o => o.OrderId).ToListAsync();
        }

        // GET: /ReceiptShipments/Create
        public async Task<IActionResult> Create()
        {
            await LoadLookups();
            return View();
        }

        // POST: /ReceiptShipments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,MovementType,Quantity,MovementDate,RelatedOrderId")] ReceiptShipment receiptShipment)
        {
            await LoadLookups();
            if (!ModelState.IsValid)
            {
                return View(receiptShipment);
            }

            _context.Add(receiptShipment);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tạo nhật ký nhập/xuất kho thành công";
            return RedirectToAction(nameof(Index));
        }

        // GET: /ReceiptShipments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var receiptShipment = await _context.ReceiptShipments.FindAsync(id);
            if (receiptShipment == null) return NotFound();
            await LoadLookups();
            return View(receiptShipment);
        }

        // POST: /ReceiptShipments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MovementId,ProductId,MovementType,Quantity,MovementDate,RelatedOrderId")] ReceiptShipment receiptShipment)
        {
            if (id != receiptShipment.MovementId) return NotFound();
            await LoadLookups();
            if (!ModelState.IsValid)
            {
                return View(receiptShipment);
            }
            try
            {
                _context.Update(receiptShipment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật nhật ký nhập/xuất kho thành công";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.ReceiptShipments.AnyAsync(e => e.MovementId == id))
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

        // GET: /ReceiptShipments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var receiptShipment = await _context.ReceiptShipments.Include(rs => rs.Product).Include(rs => rs.RelatedOrder)
                .FirstOrDefaultAsync(m => m.MovementId == id);
            if (receiptShipment == null) return NotFound();
            return View(receiptShipment);
        }

        // POST: /ReceiptShipments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var receiptShipment = await _context.ReceiptShipments.FindAsync(id);
            if (receiptShipment == null) return NotFound();
            _context.ReceiptShipments.Remove(receiptShipment);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa nhật ký nhập/xuất kho thành công";
            return RedirectToAction(nameof(Index));
        }
    }
}

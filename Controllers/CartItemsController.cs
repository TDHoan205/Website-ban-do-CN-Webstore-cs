using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Webstore.Data;
using Webstore.Models;

namespace Webstore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CartItemsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /CartItems
        public async Task<IActionResult> Index(string? search, string? sortOrder, int pageNumber = 1, int pageSize = 10)
        {
            var query = _context.CartItems
                .Include(ci => ci.Cart).ThenInclude(c => c!.Account)
                .Include(ci => ci.Product)
                .Include(ci => ci.Variant)
                .AsQueryable();
            
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(ci => ci.CartItemId.ToString().Contains(search)
                                       || (ci.Cart != null && ci.Cart.Account != null && ci.Cart.Account.Username.Contains(search))
                                       || (ci.Product != null && ci.Product.Name.Contains(search)));
            }

            // Sorting
            ViewBag.CartItemIdSortParm = sortOrder == "cartitemid" ? "cartitemid_desc" : "cartitemid";
            ViewBag.AccountSortParm = sortOrder == "account" ? "account_desc" : "account";
            ViewBag.ProductSortParm = sortOrder == "product" ? "product_desc" : "product";
            ViewBag.QuantitySortParm = sortOrder == "quantity" ? "quantity_desc" : "quantity";

            query = sortOrder switch
            {
                "cartitemid_desc" => query.OrderByDescending(ci => ci.CartItemId),
                "account" => query.OrderBy(ci => ci.Cart != null && ci.Cart.Account != null ? ci.Cart.Account.Username : ""),
                "account_desc" => query.OrderByDescending(ci => ci.Cart != null && ci.Cart.Account != null ? ci.Cart.Account.Username : ""),
                "product" => query.OrderBy(ci => ci.Product != null ? ci.Product.Name : ""),
                "product_desc" => query.OrderByDescending(ci => ci.Product != null ? ci.Product.Name : ""),
                "quantity" => query.OrderBy(ci => ci.Quantity),
                "quantity_desc" => query.OrderByDescending(ci => ci.Quantity),
                _ => query.OrderBy(ci => ci.CartItemId)
            };

            var cartItems = await PagedList<CartItem>.CreateAsync(query, pageNumber, pageSize);
            
            ViewBag.Search = search;
            ViewBag.SortOrder = sortOrder;
            ViewBag.PageSize = pageSize;
            
            return View(cartItems);
        }

        private async Task LoadLookups()
        {
            ViewBag.Carts = await _context.Carts.Include(c => c.Account).OrderByDescending(c => c.CartId).ToListAsync();
            ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            ViewBag.Variants = await _context.ProductVariants.OrderBy(v => v.VariantId).ToListAsync();
        }

        // GET: /CartItems/Create
        public async Task<IActionResult> Create()
        {
            await LoadLookups();
            return View();
        }

        // POST: /CartItems/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CartId,ProductId,VariantId,Quantity,AddedDate")] CartItem cartItem)
        {
            await LoadLookups();
            if (!ModelState.IsValid)
            {
                return View(cartItem);
            }

            _context.Add(cartItem);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tạo giỏ hàng thành công";
            return RedirectToAction(nameof(Index));
        }

        // GET: /CartItems/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var cartItem = await _context.CartItems.FindAsync(id);
            if (cartItem == null) return NotFound();
            await LoadLookups();
            return View(cartItem);
        }

        // POST: /CartItems/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CartItemId,CartId,ProductId,VariantId,Quantity,AddedDate")] CartItem cartItem)
        {
            if (id != cartItem.CartItemId) return NotFound();
            await LoadLookups();
            if (!ModelState.IsValid)
            {
                return View(cartItem);
            }
            try
            {
                _context.Update(cartItem);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật giỏ hàng thành công";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.CartItems.AnyAsync(e => e.CartItemId == id))
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

        // GET: /CartItems/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var cartItem = await _context.CartItems.Include(ci => ci.Cart).ThenInclude(c => c!.Account).Include(ci => ci.Product)
                .FirstOrDefaultAsync(m => m.CartItemId == id);
            if (cartItem == null) return NotFound();
            return View(cartItem);
        }

        // POST: /CartItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cartItem = await _context.CartItems.FindAsync(id);
            if (cartItem == null) return NotFound();
            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa giỏ hàng thành công";
            return RedirectToAction(nameof(Index));
        }
    }
}

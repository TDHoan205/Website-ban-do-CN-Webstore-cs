using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Webstore.Data;
using Webstore.Models;

namespace Webstore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AccountsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private static readonly string[] Roles = new[] { "Customer", "Admin", "Employee" };

        public AccountsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Accounts
        public IActionResult Index(string? search, string? sortOrder, int pageNumber = 1, int pageSize = 10)
        {
            var query = _context.Accounts.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(a => a.Username.Contains(search)
                                       || (a.FullName != null && a.FullName.Contains(search))
                                       || (a.Email != null && a.Email.Contains(search)));
            }

            ViewBag.UsernameSortParm = sortOrder == "username" ? "username_desc" : "username";
            ViewBag.FullNameSortParm = sortOrder == "fullname" ? "fullname_desc" : "fullname";
            ViewBag.EmailSortParm = sortOrder == "email" ? "email_desc" : "email";
            ViewBag.RoleSortParm = sortOrder == "role" ? "role_desc" : "role";

            query = sortOrder switch
            {
                "username" => query.OrderBy(a => a.Username),
                "username_desc" => query.OrderByDescending(a => a.Username),
                "fullname" => query.OrderBy(a => a.FullName),
                "fullname_desc" => query.OrderByDescending(a => a.FullName),
                "email" => query.OrderBy(a => a.Email),
                "email_desc" => query.OrderByDescending(a => a.Email),
                "role" => query.OrderBy(a => a.Role),
                "role_desc" => query.OrderByDescending(a => a.Role),
                _ => query.OrderBy(a => a.Username)
            };

            var accounts = PagedList<Account>.Create(query, pageNumber, pageSize);
            ViewBag.Search = search;
            ViewBag.SortOrder = sortOrder;
            ViewBag.PageSize = pageSize;
            return View(accounts);
        }

        // GET: /Accounts/Create
        public IActionResult Create()
        {
            ViewBag.Roles = Roles;
            return View();
        }

        // POST: /Accounts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Username,PasswordHash,Email,FullName,Phone,Address,Role")] Account account)
        {
            ViewBag.Roles = Roles;
            if (!ModelState.IsValid)
            {
                return View(account);
            }

            if (!Roles.Contains(account.Role))
            {
                ModelState.AddModelError("Role", "Vai trò không hợp lệ");
                return View(account);
            }

            var usernameExists = await _context.Accounts.AnyAsync(a => a.Username == account.Username);
            if (usernameExists)
            {
                ModelState.AddModelError("Username", "Username đã tồn tại");
                return View(account);
            }

            _context.Add(account);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tạo tài khoản thành công";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Accounts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var account = await _context.Accounts.FindAsync(id);
            if (account == null) return NotFound();

            ViewBag.Roles = Roles;
            return View(account);
        }

        // POST: /Accounts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AccountId,Username,PasswordHash,Email,FullName,Phone,Address,Role")] Account account)
        {
            if (id != account.AccountId) return NotFound();

            ViewBag.Roles = Roles;

            if (!ModelState.IsValid)
            {
                return View(account);
            }

            if (!Roles.Contains(account.Role))
            {
                ModelState.AddModelError("Role", "Vai trò không hợp lệ");
                return View(account);
            }

            var usernameExists = await _context.Accounts.AnyAsync(a => a.Username == account.Username && a.AccountId != account.AccountId);
            if (usernameExists)
            {
                ModelState.AddModelError("Username", "Username đã tồn tại");
                return View(account);
            }

            try
            {
                _context.Update(account);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật tài khoản thành công";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Accounts.AnyAsync(e => e.AccountId == id))
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

        // GET: /Accounts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == id);
            if (account == null) return NotFound();

            return View(account);
        }

        // POST: /Accounts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null) return NotFound();

            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa tài khoản thành công";
            return RedirectToAction(nameof(Index));
        }
    }
}


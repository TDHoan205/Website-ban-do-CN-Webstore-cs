using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Webstore.Data;
using Webstore.Models;

namespace Webstore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployeesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Employees
        public async Task<IActionResult> Index(string? search, string? sortOrder, int pageNumber = 1, int pageSize = 10)
        {
            var query = _context.Employees.Include(e => e.Account).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e => e.EmployeeCode.Contains(search)
                                       || (e.Position != null && e.Position.Contains(search))
                                       || (e.Department != null && e.Department.Contains(search))
                                       || (e.Account != null && e.Account.Username.Contains(search))
                                       || (e.Account != null && e.Account.FullName != null && e.Account.FullName.Contains(search)));
            }

            // Sorting
            ViewBag.CodeSortParm = sortOrder == "code" ? "code_desc" : "code";
            ViewBag.PositionSortParm = sortOrder == "position" ? "position_desc" : "position";
            ViewBag.DepartmentSortParm = sortOrder == "department" ? "department_desc" : "department";
            ViewBag.AccountSortParm = sortOrder == "account" ? "account_desc" : "account";

            query = sortOrder switch
            {
                "code_desc" => query.OrderByDescending(e => e.EmployeeCode),
                "position" => query.OrderBy(e => e.Position),
                "position_desc" => query.OrderByDescending(e => e.Position),
                "department" => query.OrderBy(e => e.Department),
                "department_desc" => query.OrderByDescending(e => e.Department),
                "account" => query.OrderBy(e => e.Account != null ? e.Account.Username : ""),
                "account_desc" => query.OrderByDescending(e => e.Account != null ? e.Account.Username : ""),
                _ => query.OrderBy(e => e.EmployeeCode)
            };

            var employees = await PagedList<Employee>.CreateAsync(query, pageNumber, pageSize);
            
            ViewBag.Search = search;
            ViewBag.SortOrder = sortOrder;
            ViewBag.PageSize = pageSize;
            
            return View(employees);
        }

        // GET: /Employees/Create
        public async Task<IActionResult> Create()
        {
            var accounts = await _context.Accounts
                .Where(a => !_context.Employees.Any(e => e.AccountId == a.AccountId))
                .OrderBy(a => a.Username)
                .ToListAsync();
            ViewBag.Accounts = accounts;
            return View();
        }

        // POST: /Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AccountId,EmployeeCode,Position,Department")] Employee employee)
        {
            var accounts = await _context.Accounts
                .Where(a => !_context.Employees.Any(e => e.AccountId == a.AccountId))
                .OrderBy(a => a.Username)
                .ToListAsync();
            ViewBag.Accounts = accounts;

            if (!ModelState.IsValid)
            {
                return View(employee);
            }

            var codeExists = await _context.Employees.AnyAsync(e => e.EmployeeCode == employee.EmployeeCode);
            if (codeExists)
            {
                ModelState.AddModelError("EmployeeCode", "Mã nhân viên đã tồn tại");
                return View(employee);
            }

            _context.Add(employee);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tạo nhân viên thành công";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            var accounts = await _context.Accounts
                .Where(a => !_context.Employees.Any(e => e.AccountId == a.AccountId) || a.AccountId == employee.AccountId)
                .OrderBy(a => a.Username)
                .ToListAsync();
            ViewBag.Accounts = accounts;
            return View(employee);
        }

        // POST: /Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EmployeeId,AccountId,EmployeeCode,Position,Department")] Employee employee)
        {
            if (id != employee.EmployeeId) return NotFound();

            var accounts = await _context.Accounts
                .Where(a => !_context.Employees.Any(e => e.AccountId == a.AccountId) || a.AccountId == employee.AccountId)
                .OrderBy(a => a.Username)
                .ToListAsync();
            ViewBag.Accounts = accounts;

            if (!ModelState.IsValid)
            {
                return View(employee);
            }

            var codeExists = await _context.Employees.AnyAsync(e => e.EmployeeCode == employee.EmployeeCode && e.EmployeeId != employee.EmployeeId);
            if (codeExists)
            {
                ModelState.AddModelError("EmployeeCode", "Mã nhân viên đã tồn tại");
                return View(employee);
            }

            try
            {
                _context.Update(employee);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật nhân viên thành công";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Employees.AnyAsync(e => e.EmployeeId == id))
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

        // GET: /Employees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees.Include(e => e.Account).FirstOrDefaultAsync(m => m.EmployeeId == id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        // POST: /Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa nhân viên thành công";
            return RedirectToAction(nameof(Index));
        }
    }
}

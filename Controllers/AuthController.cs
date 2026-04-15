using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webstore.Data;
using Webstore.Models;

namespace Webstore.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin";
                return View();
            }

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Username == username);
            if (account == null)
            {
                TempData["Error"] = "Sai thông tin đăng nhập";
                return View();
            }

            // Kiểm tra password đã hash với salt
            bool ok = false;
            if (!string.IsNullOrEmpty(account.PasswordHash) && account.PasswordHash.Contains(':'))
            {
                var parts = account.PasswordHash.Split(':');
                if (parts.Length == 2)
                {
                    var salt = parts[0];
                    var storedHash = parts[1];
                    var inputHash = Webstore.Models.Security.PasswordHasher.HashPassword(password, salt);
                    ok = CryptographicOperations.FixedTimeEquals(
                        Convert.FromHexString(inputHash),
                        Convert.FromHexString(storedHash));
                }
            }

            if (!ok)
            {
                TempData["Error"] = "Sai thông tin đăng nhập";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.AccountId.ToString()),
                new Claim(ClaimTypes.Name, account.Username),
                new Claim(ClaimTypes.Role, account.Role ?? "Customer")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

            // Xác định redirect URL
            var redirectUrl = string.Empty;
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                redirectUrl = returnUrl;
            else if (account.Role == "Admin" || account.Role == "Employee")
                redirectUrl = Url.Action("Index", "Home") ?? "/";
            else
                redirectUrl = Url.Action("Index", "Shop") ?? "/";

            // Nếu là AJAX request (X-Requested-With header), trả về JSON để client xử lý
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, redirectUrl = redirectUrl });
            }

            // Traditional form submit - redirect
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            if (account.Role == "Admin" || account.Role == "Employee")
                return RedirectToAction("Index", "Home");
            else
                return RedirectToAction("Index", "Shop");
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Account input, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Mật khẩu không được để trống");
                return View(input);
            }

            if (password.Length < 6)
            {
                ModelState.AddModelError("", "Mật khẩu phải có ít nhất 6 ký tự");
                return View(input);
            }

            // Kiểm tra username đã tồn tại chưa
            var exists = await _context.Accounts.AnyAsync(a => a.Username == input.Username);
            if (exists)
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                return View(input);
            }

            // Kiểm tra email nếu có
            if (!string.IsNullOrEmpty(input.Email))
            {
                var emailExists = await _context.Accounts.AnyAsync(a => a.Email == input.Email);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Email đã được sử dụng");
                    return View(input);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(input);
            }

            try
            {
                // Tạo salt và hash password
            var salt = Webstore.Models.Security.PasswordHasher.GenerateSalt();
            var passwordHash = Webstore.Models.Security.PasswordHasher.HashPassword(password, salt);
            
            input.PasswordHash = salt + ":" + passwordHash;
            if (string.IsNullOrEmpty(input.Role)) input.Role = "Customer";

                _context.Accounts.Add(input);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đăng ký thành công, vui lòng đăng nhập";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi đăng ký: " + ex.Message);
                return View(input);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }

}


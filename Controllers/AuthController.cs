using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webstore.Data;
using Webstore.Models;
using System.Security.Cryptography;

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

            // So sánh mật khẩu - hỗ trợ cả hash và plain text cho data cũ
            bool ok = false;
            if (!string.IsNullOrEmpty(account.PasswordHash))
            {
                // Thử hash mới trước
                ok = PasswordHasher.VerifyHash(password, account.PasswordHash);
                // Nếu không đúng, thử so sánh plain text (backward compatible)
                if (!ok)
                {
                    ok = string.Equals(password, account.PasswordHash, StringComparison.OrdinalIgnoreCase);
                    // Nếu plain text đúng, tự động cập nhật sang hash cho lần sau
                    if (ok)
                    {
                        account.PasswordHash = PasswordHasher.HashPassword(password);
                        await _context.SaveChangesAsync();
                    }
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
                // Mã hóa mật khẩu
                input.PasswordHash = PasswordHasher.HashPassword(password);
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

    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        public static string HashPassword(string password)
        {
            byte[] salt = new byte[SaltSize];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] hash;
            using (var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(password, salt, Iterations, System.Security.Cryptography.HashAlgorithmName.SHA256))
            {
                hash = pbkdf2.GetBytes(HashSize);
            }

            byte[] hashBytes = new byte[SaltSize + HashSize];
            Array.Copy(salt, 0, hashBytes, 0, SaltSize);
            Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

            return Convert.ToBase64String(hashBytes);
        }

        public static bool VerifyHash(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash)) return false;

            try
            {
                byte[] hashBytes = Convert.FromBase64String(storedHash);
                if (hashBytes.Length != SaltSize + HashSize) return false;

                byte[] salt = new byte[SaltSize];
                Array.Copy(hashBytes, 0, salt, 0, SaltSize);

                byte[] storedHashValue = new byte[HashSize];
                Array.Copy(hashBytes, SaltSize, storedHashValue, 0, HashSize);

                byte[] computedHash;
                using (var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(password, salt, Iterations, System.Security.Cryptography.HashAlgorithmName.SHA256))
                {
                    computedHash = pbkdf2.GetBytes(HashSize);
                }

                return CryptographicOperations.FixedTimeEquals(storedHashValue, computedHash);
            }
            catch
            {
                return false;
            }
        }
    }
}


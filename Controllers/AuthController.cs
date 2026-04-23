using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webstore.Data;
using Webstore.Models;
using Webstore.Services;

namespace Webstore.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public AuthController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                var errorMsg = "Vui lòng nhập đầy đủ thông tin";
                if (isAjax)
                    return Json(new { success = false, error = errorMsg });
                TempData["Error"] = errorMsg;
                return View();
            }

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Username == username);
            if (account == null)
            {
                var errorMsg = "Sai thông tin đăng nhập";
                if (isAjax)
                    return Json(new { success = false, error = errorMsg });
                TempData["Error"] = errorMsg;
                return View();
            }

            // Kiểm tra password đã hash với salt
            bool ok = false;
            if (!string.IsNullOrEmpty(account.PasswordHash) && account.PasswordHash.Contains(':'))
            {
                // Định dạng salt:hash
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
            else if (!string.IsNullOrEmpty(account.PasswordHash))
            {
                // Fallback: password lưu dạng plaintext (database cũ)
                ok = account.PasswordHash == password;
                if (ok)
                {
                    // Tự động nâng cấp lên dạng hash
                    var newSalt = Webstore.Models.Security.PasswordHasher.GenerateSalt();
                    var newHash = Webstore.Models.Security.PasswordHasher.HashPassword(password, newSalt);
                    account.PasswordHash = newSalt + ":" + newHash;
                    await _context.SaveChangesAsync();
                }
            }

            if (!ok)
            {
                var errorMsg = "Sai thông tin đăng nhập";
                if (isAjax)
                    return Json(new { success = false, error = errorMsg });
                TempData["Error"] = errorMsg;
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

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (string.IsNullOrWhiteSpace(email))
            {
                var errorMsg = "Vui lòng nhập địa chỉ email";
                if (isAjax)
                    return Json(new { success = false, error = errorMsg });
                TempData["Error"] = errorMsg;
                return View();
            }

            if (!email.Contains('@') || !email.Contains('.'))
            {
                var errorMsg = "Địa chỉ email không hợp lệ";
                if (isAjax)
                    return Json(new { success = false, error = errorMsg });
                TempData["Error"] = errorMsg;
                return View();
            }

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == email);
            if (account == null)
            {
                // Always show success message to prevent email enumeration
                if (isAjax)
                    return Json(new { success = true, message = "Nếu email tồn tại trong hệ thống, hướng dẫn đặt lại mật khẩu đã được gửi." });
                TempData["Success"] = "Nếu email tồn tại trong hệ thống, hướng dẫn đặt lại mật khẩu đã được gửi.";
                return RedirectToAction("Login");
            }

            // Generate a secure reset token
            var resetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            account.ResetToken = resetToken;
            account.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);
            await _context.SaveChangesAsync();

            // Build reset link
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var resetLink = $"{baseUrl}/Auth/ResetPassword?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(email)}";

            // Send email
            var emailSent = await _emailService.SendPasswordResetEmailAsync(email, resetLink);

            if (isAjax)
            {
                if (emailSent)
                    return Json(new { success = true, message = "Đã gửi hướng dẫn đặt lại mật khẩu đến email của bạn. Vui lòng kiểm tra hộp thư (bao gồm thư rác)." });
                else
                    return Json(new { success = true, message = "Nếu email tồn tại trong hệ thống, hướng dẫn đặt lại mật khẩu đã được gửi." });
            }

            TempData["Success"] = "Nếu email tồn tại trong hệ thống, hướng dẫn đặt lại mật khẩu đã được gửi.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ResetPassword(string? token, string? email)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Liên kết đặt lại mật khẩu không hợp lệ.";
                return RedirectToAction("ForgotPassword");
            }

            ViewBag.Token = token;
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string token, string newPassword, string confirmPassword)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
            {
                var errorMsg = "Liên kết đặt lại mật khẩu không hợp lệ.";
                if (isAjax)
                    return Json(new { success = false, error = errorMsg });
                TempData["Error"] = errorMsg;
                return RedirectToAction("ForgotPassword");
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                var errorMsg = "Mật khẩu mới phải có ít nhất 6 ký tự";
                ViewBag.Token = token;
                ViewBag.Email = email;
                if (isAjax)
                    return Json(new { success = false, error = errorMsg });
                ModelState.AddModelError("", errorMsg);
                return View();
            }

            if (newPassword != confirmPassword)
            {
                var errorMsg = "Mật khẩu xác nhận không khớp";
                ViewBag.Token = token;
                ViewBag.Email = email;
                if (isAjax)
                    return Json(new { success = false, error = errorMsg });
                ModelState.AddModelError("", errorMsg);
                return View();
            }

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == email);
            if (account == null || account.ResetToken != token)
            {
                var errorMsg = "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
                if (isAjax)
                    return Json(new { success = false, error = errorMsg });
                TempData["Error"] = errorMsg;
                return RedirectToAction("ForgotPassword");
            }

            if (account.ResetTokenExpiry == null || account.ResetTokenExpiry < DateTime.UtcNow)
            {
                var errorMsg = "Liên kết đặt lại mật khẩu đã hết hạn. Vui lòng yêu cầu đặt lại mật khẩu mới.";
                if (isAjax)
                    return Json(new { success = false, error = errorMsg });
                TempData["Error"] = errorMsg;
                return RedirectToAction("ForgotPassword");
            }

            // Update password
            var salt = Webstore.Models.Security.PasswordHasher.GenerateSalt();
            var passwordHash = Webstore.Models.Security.PasswordHasher.HashPassword(newPassword, salt);
            account.PasswordHash = salt + ":" + passwordHash;
            account.ResetToken = null;
            account.ResetTokenExpiry = null;
            await _context.SaveChangesAsync();

            if (isAjax)
                return Json(new { success = true, message = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập với mật khẩu mới." });

            TempData["Success"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập với mật khẩu mới.";
            return RedirectToAction("Login");
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


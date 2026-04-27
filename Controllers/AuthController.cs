using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Webstore.Models;
using Webstore.Services;

namespace Webstore.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IEmailService _emailService;

        public AuthController(IAccountService accountService, IEmailService emailService)
        {
            _accountService = accountService;
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
                if (isAjax) return Json(new { success = false, error = errorMsg });
                TempData["Error"] = errorMsg;
                return View();
            }

            var isValid = await _accountService.ValidateCredentialsAsync(username, password);
            if (!isValid)
            {
                var errorMsg = "Sai thông tin đăng nhập";
                if (isAjax) return Json(new { success = false, error = errorMsg });
                TempData["Error"] = errorMsg;
                return View();
            }

            var account = await _accountService.GetAccountByUsernameAsync(username);
            if (account == null)
            {
                account = await _accountService.GetAccountByEmailAsync(username);
            }
            if (account == null)
            {
                var errorMsg = "Sai thông tin đăng nhập";
                if (isAjax) return Json(new { success = false, error = errorMsg });
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
            var authProperties = new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

            var redirectUrl = account.Role == "Admin" || account.Role == "Employee"
                ? Url.Action("Index", "Home") : Url.Action("Index", "Shop");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) redirectUrl = returnUrl;

            if (isAjax) return Json(new { success = true, redirectUrl = redirectUrl });
            return Redirect(redirectUrl!);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Account input, string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                ModelState.AddModelError("", "Mật khẩu phải có ít nhất 6 ký tự");
                return View(input);
            }

            if (await _accountService.GetAccountByUsernameAsync(input.Username) != null)
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                return View(input);
            }

            // Check if email already exists (normalize before checking)
            if (!string.IsNullOrWhiteSpace(input.Email))
            {
                var normalizedEmail = input.Email.Trim().ToLowerInvariant();
                input.Email = normalizedEmail; // Normalize the input
                if (await _accountService.GetAccountByEmailAsync(normalizedEmail) != null)
                {
                    ModelState.AddModelError("Email", "Email đã được sử dụng");
                    return View(input);
                }
            }

            try
            {
                await _accountService.RegisterAsync(input, password);

                TempData["Success"] = "Đăng ký thành công, vui lòng đăng nhập";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi đăng ký: " + ex.Message);
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

            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@') || !email.Contains('.'))
            {
                var errorMsg = "Vui lòng nhập địa chỉ email hợp lệ";
                if (isAjax) return Json(new { success = false, error = errorMsg });
                TempData["Error"] = errorMsg;
                return View();
            }

            // Always show success message to prevent email enumeration
            var account = await _accountService.GetAccountByEmailAsync(email);

            if (account != null)
            {
                // Generate reset token
                await _accountService.GenerateResetTokenAsync(email);

                // Build reset link
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var resetLink = $"{baseUrl}/Auth/ResetPassword?token={Uri.EscapeDataString(account.ResetToken ?? "")}&email={Uri.EscapeDataString(email)}";

                // Send email
                await _emailService.SendPasswordResetEmailAsync(email, resetLink);
            }

            if (isAjax)
            {
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

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                var errorMsg = "Mật khẩu mới phải có ít nhất 6 ký tự";
                ViewBag.Token = token;
                ViewBag.Email = email;
                if (isAjax) return Json(new { success = false, error = errorMsg });
                ModelState.AddModelError("", errorMsg);
                return View();
            }

            if (newPassword != confirmPassword)
            {
                var errorMsg = "Mật khẩu xác nhận không khớp";
                ViewBag.Token = token;
                ViewBag.Email = email;
                if (isAjax) return Json(new { success = false, error = errorMsg });
                ModelState.AddModelError("", errorMsg);
                return View();
            }

            var isValid = await _accountService.ValidateResetTokenAsync(email, token);
            if (!isValid)
            {
                var errorMsg = "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
                if (isAjax) return Json(new { success = false, error = errorMsg });
                TempData["Error"] = errorMsg;
                return RedirectToAction("ForgotPassword");
            }

            var success = await _accountService.ResetPasswordAsync(email, token, newPassword);
            if (!success)
            {
                var errorMsg = "Không thể đặt lại mật khẩu. Vui lòng thử lại.";
                if (isAjax) return Json(new { success = false, error = errorMsg });
                TempData["Error"] = errorMsg;
                return RedirectToAction("ForgotPassword");
            }

            if (isAjax)
            {
                return Json(new { success = true, message = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập với mật khẩu mới." });
            }

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


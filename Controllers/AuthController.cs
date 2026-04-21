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
        private readonly Webstore.Services.IAccountService _accountService;

        public AuthController(Webstore.Services.IAccountService accountService)
        {
            _accountService = accountService;
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
                var errorMsg = "Lỗi hệ thống: không tìm thấy tài khoản sau khi xác thực";
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


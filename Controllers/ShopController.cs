using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Webstore.Data;
using Webstore.Models;
using Webstore.Services;

namespace Webstore.Controllers
{
    public class ShopController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IProductService _productService;
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly IAccountService _accountService;
        private readonly IWebHostEnvironment _env;

        public ShopController(
            IConfiguration configuration,
            IProductService productService,
            ICartService cartService,
            IOrderService orderService,
            IAccountService accountService,
            IWebHostEnvironment env)
        {
            _configuration = configuration;
            _productService = productService;
            _cartService = cartService;
            _orderService = orderService;
            _accountService = accountService;
            _env = env;
        }

        // GET: /Shop - Trang chủ shop
        public async Task<IActionResult> Index(int newPage = 1, int hotPage = 1)
        {
            ViewBag.NewProducts = await _productService.GetFeaturedProductsAsync("new", 100);
            ViewBag.HotProducts = await _productService.GetFeaturedProductsAsync("hot", 100);
            ViewBag.DealProducts = await _productService.GetFeaturedProductsAsync("deal", 100);
            ViewBag.Categories = (await _productService.GetAllCategoriesAsync()).ToList();
            
            ViewBag.NewProductsPage = newPage;
            ViewBag.HotProductsPage = hotPage;

            return View();
        }

        // GET: /Shop/GetFeaturedProductsPartial (AJAX)
        public async Task<IActionResult> GetFeaturedProductsPartial(int page = 1)
        {
            var products = await _productService.GetFeaturedProductsAsync("new", 100);
            ViewBag.NewProducts = products;
            ViewBag.NewProductsPage = page;
            ViewBag.TotalNewPages = (int)Math.Ceiling(products.Count() / 4.0);
            var displayProducts = products.Skip((page - 1) * 4).Take(4).ToList();
            return PartialView("_NewArrivalsPartial", displayProducts);
        }

        // GET: /Shop/GetHotProductsPartial (AJAX)
        public async Task<IActionResult> GetHotProductsPartial(int page = 1)
        {
            var products = await _productService.GetFeaturedProductsAsync("hot", 100);
            ViewBag.HotProducts = products;
            ViewBag.HotProductsPage = page;
            ViewBag.TotalHotPages = (int)Math.Ceiling(products.Count() / 4.0);
            var displayProducts = products.Skip((page - 1) * 4).Take(4).ToList();
            return PartialView("_HotProductsPartial", displayProducts);
        }

        // GET: /Shop/Products - Danh sách sản phẩm
        public async Task<IActionResult> Products(string? search, int? categoryId, string? sortBy, int page = 1, int pageSize = 8, decimal? minPrice = null, decimal? maxPrice = null, string? filter = null)
        {
            var pList = await _productService.GetProductsAsync(search, categoryId, sortBy, page, pageSize, minPrice, maxPrice, filter);
            var filters = await _productService.GetFiltersAsync(categoryId);

            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.SortBy = sortBy;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Filter = filter;
            ViewBag.Filters = filters;
            ViewBag.Categories = await _productService.GetAllCategoriesAsync();

            return View(pList);
        }

        // GET: /Shop/Product/{id} - Chi tiết sản phẩm
        public async Task<IActionResult> Product(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.RelatedProducts = await _productService.GetRelatedProductsAsync(id, product.CategoryId ?? 0, 4);
            ViewBag.ProductImages = GetProductImageUrls(product);

            return View(product);
        }

        private List<string> GetProductImageUrls(Product product)
        {
            var results = new List<string>();

            if (!string.IsNullOrWhiteSpace(product.ImageUrl))
            {
                results.Add(NormalizeUrl(product.ImageUrl));
            }

            // Try to auto-discover additional images in the same folder with the same base name.
            // Example: /images/products/iPhone_15.png -> also match iPhone_15_1.png, iPhone_15-side.webp, ...
            if (!string.IsNullOrWhiteSpace(product.ImageUrl))
            {
                try
                {
                    var webRoot = _env.WebRootPath ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(webRoot))
                    {
                        var normalized = NormalizeUrl(product.ImageUrl);
                        var relativePath = normalized.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                        var physicalPath = Path.Combine(webRoot, relativePath);
                        var directory = Path.GetDirectoryName(physicalPath);

                        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                        {
                            var baseName = Path.GetFileNameWithoutExtension(physicalPath);

                            var discovered = Directory.EnumerateFiles(directory)
                                .Where(f => Path.GetFileNameWithoutExtension(f)
                                    .StartsWith(baseName, StringComparison.OrdinalIgnoreCase))
                                .OrderBy(f => f)
                                .Select(f => "/" + Path.GetRelativePath(webRoot, f).Replace('\\', '/'))
                                .ToList();

                            foreach (var url in discovered)
                            {
                                var u = NormalizeUrl(url);
                                if (!results.Any(r => string.Equals(r, u, StringComparison.OrdinalIgnoreCase)))
                                {
                                    results.Add(u);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Non-blocking: fall back to ImageUrl only
                }
            }

            if (results.Count == 0)
            {
                results.Add("/images/products/placeholder.svg");
            }

            // Ensure at least 5 thumbnails as requested.
            while (results.Count < 5)
            {
                results.Add(results[0]);
            }

            return results;
        }

        private static string NormalizeUrl(string url)
        {
            var u = url.Trim();
            if (u.StartsWith("~")) u = u.TrimStart('~');
            if (!u.StartsWith('/')) u = "/" + u;
            return u;
        }

        // GET: /Shop/Cart - Giỏ hàng
        public async Task<IActionResult> Cart()
        {
            var items = await _cartService.GetCartItemsAsync();
            return View(items);
        }

        // POST: /Shop/AddToCart - Thêm vào giỏ hàng
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1, int? variantId = null)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Json(new { success = false, requiresLogin = true, message = "Vui lòng đăng nhập để mua hàng." });
            }

            try
            {
                await _cartService.AddToCartAsync(productId, variantId, quantity);
                var count = await _cartService.GetCartCount();
                return Json(new { success = true, message = "Đã thêm vào giỏ hàng!", cartCount = count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: /Shop/RestorePendingCart - Khôi phục giỏ hàng sau khi đăng nhập
        [HttpPost]
        public async Task<IActionResult> RestorePendingCart()
        {
            var count = await _cartService.GetCartCount();
            return Json(new { success = true, cartCount = count });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UpdateCart(int productId, int? variantId, int quantity)
        {
            try
            {
                await _cartService.UpdateQuantityAsync(productId, variantId, quantity);
                var count = await _cartService.GetCartCount();
                return Json(new { success = true, newQuantity = quantity, cartCount = count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RemoveFromCart(int productId, int? variantId)
        {
            await _cartService.RemoveFromCartAsync(productId, variantId);
            var count = await _cartService.GetCartCount();
            return Json(new { success = true, cartCount = count });
        }

        // GET: /Shop/Checkout - Thanh toán
        public async Task<IActionResult> Checkout(string? selected)
        {
            var cartItems = await _cartService.GetCartItemsAsync();

            // Load payment info from configuration
            var paymentSection = HttpContext.RequestServices.GetService<IConfiguration>()?.GetSection("PaymentInfo");
            ViewBag.PaymentInfo = new
            {
                BankName = paymentSection?["BankName"] ?? "MB Bank",
                BankBin = paymentSection?["BankBin"] ?? "mbbank",
                AccountNumber = paymentSection?["AccountNumber"] ?? "123456789012",
                AccountName = paymentSection?["AccountName"] ?? "WEBSTORE SHOP"
            };

            var sandboxEnabled = _configuration.GetValue<bool>("PaymentSandbox:Enabled");
            var tmnCode = _configuration.GetSection("VnPay")["TmnCode"];
            var hashSecret = _configuration.GetSection("VnPay")["HashSecret"];
            ViewBag.IsSandboxPayment = sandboxEnabled &&
                (string.IsNullOrWhiteSpace(tmnCode) || string.IsNullOrWhiteSpace(hashSecret));

            if (!string.IsNullOrWhiteSpace(selected))
            {
                try
                {
                    var selectedPairs = selected.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Split('-'))
                        .Where(parts => parts.Length == 2)
                        .Select(parts => new { pId = int.Parse(parts[0]), vId = parts[1] == "0" ? (int?)null : int.Parse(parts[1]) })
                        .ToList();

                    if (selectedPairs.Any())
                    {
                        cartItems = cartItems.Where(c => selectedPairs.Any(s => s.pId == c.ProductId && s.vId == c.VariantId)).ToList();
                    }
                }
                catch { }
            }

            if (!cartItems.Any()) return RedirectToAction("Cart");
            return View(cartItems);
        }

        // POST: /Shop/PlaceOrder - Đặt hàng
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest req)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để đặt hàng." });
            }

            if (req == null || string.IsNullOrWhiteSpace(req.CustomerName) || string.IsNullOrWhiteSpace(req.CustomerPhone) || string.IsNullOrWhiteSpace(req.CustomerAddress))
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin bắt buộc." });
            }

            try
            {
                var accountId = GetCurrentAccountId();
                var order = await _orderService.CreateOrderAsync(req, accountId);

                if (string.Equals(req.PaymentMethod, "vnpay", StringComparison.OrdinalIgnoreCase))
                {
                    var paymentUrl = BuildVnPayPaymentUrl(order);
                    if (string.IsNullOrWhiteSpace(paymentUrl))
                    {
                        paymentUrl = BuildSandboxPaymentUrl(order);
                        if (string.IsNullOrWhiteSpace(paymentUrl))
                        {
                            return Json(new { success = false, message = "Không thể khởi tạo cổng thanh toán VNPAY." });
                        }
                    }

                    return Json(new { success = true, orderId = order.OrderId, requiresRedirect = true, paymentUrl });
                }

                return Json(new { success = true, orderId = order.OrderId, message = "Đặt hàng thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: /Shop/PaymentReturn - Return URL từ VNPAY sau khi user thanh toán xong
        [HttpGet]
        public async Task<IActionResult> PaymentReturn()
        {
            var hashSecret = _configuration.GetSection("VnPay")["HashSecret"];
            if (string.IsNullOrWhiteSpace(hashSecret))
            {
                TempData["Error"] = "Thiếu cấu hình HashSecret của VNPAY.";
                return RedirectToAction("Checkout");
            }

            var data = ExtractVnPayResponseData(Request.Query);
            if (!IsValidVnPaySignature(data, hashSecret))
            {
                TempData["Error"] = "Xác thực chữ ký thanh toán thất bại.";
                return RedirectToAction("Checkout");
            }

            if (!int.TryParse(data.GetValueOrDefault("vnp_TxnRef"), out var orderId))
            {
                TempData["Error"] = "Không tìm thấy mã đơn hàng hợp lệ.";
                return RedirectToAction("Checkout");
            }

            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                TempData["Error"] = "Đơn hàng không tồn tại.";
                return RedirectToAction("Checkout");
            }

            var responseCode = data.GetValueOrDefault("vnp_ResponseCode");
            var transactionStatus = data.GetValueOrDefault("vnp_TransactionStatus");
            var isSuccess = responseCode == "00" && transactionStatus == "00";

                await _orderService.UpdateOrderStatusAsync(orderId, "Confirmed");

            if (isSuccess && User.Identity?.IsAuthenticated == true && GetCurrentAccountId() == order.AccountId)
            {
                await _cartService.ClearCart();
            }

            if (User.Identity?.IsAuthenticated != true)
            {
                var returnUrl = Url.Action("OrderDetail", "Shop", new { id = orderId, payment = isSuccess ? "success" : "failed" }) ?? "/Shop/OrderHistory";
                return RedirectToAction("Login", "Auth", new { returnUrl });
            }

            if (GetCurrentAccountId() != order.AccountId)
            {
                return RedirectToAction("OrderHistory");
            }

            return RedirectToAction("OrderDetail", new { id = orderId, payment = isSuccess ? "success" : "failed" });
        }

        // GET: /Shop/PaymentIpn - Callback server-to-server từ VNPAY
        [HttpGet]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> PaymentIpn()
        {
            var hashSecret = _configuration.GetSection("VnPay")["HashSecret"];
            if (string.IsNullOrWhiteSpace(hashSecret))
            {
                return Json(new { RspCode = "99", Message = "Missing configuration" });
            }

            var data = ExtractVnPayResponseData(Request.Query);
            if (!IsValidVnPaySignature(data, hashSecret))
            {
                return Json(new { RspCode = "97", Message = "Invalid signature" });
            }

            if (!int.TryParse(data.GetValueOrDefault("vnp_TxnRef"), out var orderId))
            {
                return Json(new { RspCode = "01", Message = "Order not found" });
            }

            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                return Json(new { RspCode = "01", Message = "Order not found" });
            }

            if (!long.TryParse(data.GetValueOrDefault("vnp_Amount"), out var amountRaw) || amountRaw <= 0)
            {
                return Json(new { RspCode = "04", Message = "Invalid amount" });
            }

            var paidAmount = amountRaw / 100m;
            if (Math.Abs(order.TotalAmount - paidAmount) > 0.01m)
            {
                return Json(new { RspCode = "04", Message = "Invalid amount" });
            }

            if (order.Status == "Confirmed")
            {
                return Json(new { RspCode = "02", Message = "Order already confirmed" });
            }

            var responseCode = data.GetValueOrDefault("vnp_ResponseCode");
            var transactionStatus = data.GetValueOrDefault("vnp_TransactionStatus");
            var isSuccess = responseCode == "00" && transactionStatus == "00";

            if (isSuccess)
            {
                await _orderService.UpdateOrderStatusAsync(orderId, "Confirmed");
            }

            return Json(new { RspCode = "00", Message = "Confirm Success" });
        }

        // GET: /Shop/SandboxAutoPay - Giả lập thanh toán tự động cho môi trường demo/sinh viên
        [HttpGet]
        public async Task<IActionResult> SandboxAutoPay(int orderId)
        {
            var sandboxEnabled = _configuration.GetValue<bool>("PaymentSandbox:Enabled");
            if (!sandboxEnabled)
            {
                return RedirectToAction("Checkout");
            }

            if (User.Identity?.IsAuthenticated != true)
            {
                var returnUrl = Url.Action("SandboxAutoPay", "Shop", new { orderId }) ?? "/Shop/Checkout";
                return RedirectToAction("Login", "Auth", new { returnUrl });
            }

            var accountId = GetCurrentAccountId();
            var orderDetails = await _orderService.GetOrderDetailsAsync(orderId, accountId);
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng cần thanh toán.";
                return RedirectToAction("OrderHistory");
            }

            if (order.Status != "Confirmed")
            {
                await _orderService.UpdateOrderStatusAsync(orderId, "Confirmed");

                await _cartService.ClearCart();
                TempData["Success"] = "Demo: hệ thống đã tự động xác nhận thanh toán thành công.";
            }

            return RedirectToAction("OrderDetail", new { id = orderId, payment = "success" });
        }

        // POST: /Shop/ConfirmPayment - Xác nhận thanh toán thực sự (clear cart sau khi user xác nhận đã chuyển khoản)
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ConfirmPayment(int orderId)
        {
            if (orderId <= 0)
            {
                return Json(new { success = false, message = "Mã đơn hàng không hợp lệ." });
            }

            var accountId = GetCurrentAccountId();
            var order = await _orderService.GetOrderDetailsAsync(orderId, accountId);
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng hoặc đơn hàng không thuộc về bạn." });
            }

            // Prevent double confirmation
            if (order.Status == "Confirmed" || order.Status == "Paid")
            {
                return Json(new { success = false, message = "Đơn hàng đã được xác nhận thanh toán trước đó." });
            }

            try
            {
                await _orderService.ConfirmPaymentAsync(orderId);
                return Json(new { success = true, message = "Xác nhận thanh toán thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // GET: /Shop/CheckPaymentStatus - Kiểm tra trạng thái thanh toán qua Sepay API
        [HttpGet]
        public async Task<IActionResult> CheckPaymentStatus(string transferCode, decimal amount)
        {
            if (string.IsNullOrWhiteSpace(transferCode) || amount <= 0)
            {
                return Json(new { paid = false, message = "Thông tin không hợp lệ." });
            }

            var sepayApiKey = _configuration.GetSection("Sepay")["ApiKey"];
            var sepayAccountNumber = _configuration.GetSection("Sepay")["AccountNumber"];

            // If Sepay is not configured, fall back to sandbox mode (auto-success for demo)
            if (string.IsNullOrWhiteSpace(sepayApiKey))
            {
                var sandboxEnabled = _configuration.GetValue<bool>("PaymentSandbox:Enabled");
                if (sandboxEnabled)
                {
                    // In sandbox/demo mode without Sepay, never return paid=true automatically
                    // User must use the manual confirm button
                    return Json(new { paid = false, message = "Sepay chưa được cấu hình. Dùng nút xác nhận thủ công.", sandbox = true });
                }
                return Json(new { paid = false, message = "Chưa cấu hình API thanh toán tự động." });
            }

            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {sepayApiKey}");
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                // Query Sepay API for recent transactions matching our account
                var apiUrl = $"https://my.sepay.vn/userapi/transactions/list?account_number={sepayAccountNumber}&limit=20";
                var response = await httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { paid = false, message = "Không thể kết nối Sepay API." });
                }

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = System.Text.Json.JsonDocument.Parse(content);

                if (!jsonDoc.RootElement.TryGetProperty("transactions", out var transactions))
                {
                    return Json(new { paid = false });
                }

                var roundedAmount = Math.Round(amount, 0, MidpointRounding.AwayFromZero);

                foreach (var tx in transactions.EnumerateArray())
                {
                    // Check transaction content contains our transfer code
                    var txContent = tx.TryGetProperty("transaction_content", out var contentProp)
                        ? contentProp.GetString() ?? ""
                        : "";

                    var txAmount = 0m;
                    if (tx.TryGetProperty("amount_in", out var amountProp))
                    {
                        decimal.TryParse(amountProp.ToString(), out txAmount);
                    }

                    // Match: transfer code found in content AND amount matches
                    if (txContent.Contains(transferCode, StringComparison.OrdinalIgnoreCase)
                        && Math.Abs(txAmount - roundedAmount) < 1m)
                    {
                        return Json(new { paid = true, message = "Thanh toán thành công!" });
                    }
                }

                return Json(new { paid = false });
            }
            catch (TaskCanceledException)
            {
                return Json(new { paid = false, message = "Hết thời gian kết nối Sepay." });
            }
            catch (Exception)
            {
                return Json(new { paid = false, message = "Lỗi kiểm tra thanh toán." });
            }
        }

        public async Task<IActionResult> GetCartCount()
        {
            var count = await _cartService.GetCartCount();
            return Json(count);
        }

        private int GetCurrentAccountId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(claim) && int.TryParse(claim, out var accountId))
            {
                return accountId;
            }
            return 1;
        }



        private string BuildVnPayPaymentUrl(Order order)
        {
            var vnpay = _configuration.GetSection("VnPay");
            var tmnCode = vnpay["TmnCode"];
            var hashSecret = vnpay["HashSecret"];
            var baseUrl = vnpay["BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            var version = vnpay["Version"] ?? "2.1.0";
            var command = vnpay["Command"] ?? "pay";
            var currCode = vnpay["CurrCode"] ?? "VND";
            var locale = vnpay["Locale"] ?? "vn";
            var returnPath = vnpay["ReturnUrl"] ?? "/Shop/PaymentReturn";

            if (string.IsNullOrWhiteSpace(tmnCode) || string.IsNullOrWhiteSpace(hashSecret))
            {
                return string.Empty;
            }

            var now = DateTime.Now;
            var amount = (long)Math.Round(order.TotalAmount * 100m, 0, MidpointRounding.AwayFromZero);
            var returnUrl = returnPath.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? returnPath
                : $"{Request.Scheme}://{Request.Host}{(returnPath.StartsWith('/') ? returnPath : "/" + returnPath)}";

            var ipAddr = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrWhiteSpace(ipAddr))
            {
                ipAddr = "127.0.0.1";
            }

            var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["vnp_Version"] = version,
                ["vnp_Command"] = command,
                ["vnp_TmnCode"] = tmnCode,
                ["vnp_Amount"] = amount.ToString(),
                ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss"),
                ["vnp_CurrCode"] = currCode,
                ["vnp_IpAddr"] = ipAddr,
                ["vnp_Locale"] = locale,
                ["vnp_OrderInfo"] = $"Thanh toan don hang {order.OrderId}",
                ["vnp_OrderType"] = "other",
                ["vnp_ReturnUrl"] = returnUrl,
                ["vnp_TxnRef"] = order.OrderId.ToString(),
                ["vnp_ExpireDate"] = now.AddMinutes(15).ToString("yyyyMMddHHmmss")
            };

            var signData = BuildVnPayQuery(vnpParams, false);
            var secureHash = ComputeHmacSha512(hashSecret, signData);
            vnpParams["vnp_SecureHash"] = secureHash;

            return $"{baseUrl}?{BuildVnPayQuery(vnpParams, true)}";
        }

        private string BuildSandboxPaymentUrl(Order order)
        {
            var sandboxEnabled = _configuration.GetValue<bool>("PaymentSandbox:Enabled");
            if (!sandboxEnabled)
            {
                return string.Empty;
            }

            return Url.Action("SandboxAutoPay", "Shop", new { orderId = order.OrderId }, Request.Scheme) ?? string.Empty;
        }

        private static string BuildVnPayQuery(SortedDictionary<string, string> data, bool encode)
        {
            return string.Join("&", data
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                .Select(kv => $"{kv.Key}={(encode ? Uri.EscapeDataString(kv.Value) : kv.Value)}"));
        }

        private static string ComputeHmacSha512(string key, string data)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            using var hmac = new HMACSHA512(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        private static Dictionary<string, string> ExtractVnPayResponseData(IQueryCollection query)
        {
            var data = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var key in query.Keys)
            {
                if (key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
                {
                    data[key] = query[key].ToString();
                }
            }
            return data;
        }

        private static bool IsValidVnPaySignature(Dictionary<string, string> data, string hashSecret)
        {
            if (!data.TryGetValue("vnp_SecureHash", out var secureHash) || string.IsNullOrWhiteSpace(secureHash))
            {
                return false;
            }

            var signingData = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var kv in data)
            {
                if (kv.Key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase) ||
                    kv.Key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                signingData[kv.Key] = kv.Value;
            }

            var raw = BuildVnPayQuery(signingData, false);
            var expectedHash = ComputeHmacSha512(hashSecret, raw);
            return secureHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
        }

        // Support, etc...

        // GET: /Shop/Support - Trang hỗ trợ
        public IActionResult Support()
        {
            return View();
        }

        // GET: /Shop/Search - Tìm kiếm realtime (AJAX)
        public async Task<IActionResult> Search(string? q)
        {
            var results = await _productService.SearchRealtime(q ?? "", 8);
            return Json(results);
        }

        // GET: /Shop/OrderHistory - Lịch sử đơn hàng
        public async Task<IActionResult> OrderHistory()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Auth");
            }

            var accountId = GetCurrentAccountId();
            var orders = await _orderService.GetOrderHistoryAsync(accountId);

            return View(orders);
        }

        // GET: /Shop/OrderDetail/{id} - Chi tiết đơn hàng
        public async Task<IActionResult> OrderDetail(int id, string? payment = null)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Auth");
            }

            var accountId = GetCurrentAccountId();
            var order = await _orderService.GetOrderDetailsAsync(id, accountId);

            if (order == null)
            {
                return NotFound();
            }

            ViewBag.PaymentResult = payment;
            return View(order);
        }

        // GET: /Shop/Profile - Thông tin cá nhân
        public async Task<IActionResult> Profile()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Auth");
            }

            var accountId = GetCurrentAccountId();
            var account = await _accountService.GetAccountByIdAsync(accountId);
            if (account == null)
            {
                return NotFound();
            }

            return View(account);
        }

        // POST: /Shop/UpdateProfile - Cập nhật thông tin cá nhân
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string fullName, string email, string phone, string address)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." });
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                return Json(new { success = false, message = "Vui lòng nhập họ và tên." });
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                return Json(new { success = false, message = "Vui lòng nhập số điện thoại." });
            }

            if (!string.IsNullOrWhiteSpace(email) && !Regex.IsMatch(email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                return Json(new { success = false, message = "Email không hợp lệ. Vui lòng nhập đúng định dạng." });
            }

            if (!Regex.IsMatch(phone.Trim(), @"^0\d{9}$"))
            {
                return Json(new { success = false, message = "Số điện thoại không hợp lệ. Vui lòng nhập đúng định dạng (10 chữ số, bắt đầu bằng 0)." });
            }

            // Validate name: at least 2 chars
            if (fullName.Trim().Length < 2)
            {
                return Json(new { success = false, message = "Họ và tên quá ngắn (ít nhất 2 ký tự)." });
            }

            var accountId = GetCurrentAccountId();

            try
            {
                var safeEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
                var safeAddress = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
                await _accountService.UpdateProfileAsync(accountId, fullName.Trim(), safeEmail, phone.Trim(), safeAddress);
                return Json(new { success = true, message = "Cập nhật thông tin thành công!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Cập nhật thất bại. Vui lòng thử lại." });
            }
        }
    }
}

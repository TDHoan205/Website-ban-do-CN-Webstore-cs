using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Webstore.Data;
using Webstore.Models;

namespace Webstore.Controllers
{
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public ShopController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: /Shop - Trang chủ shop
        public IActionResult Index()
        {
            // Lấy tất cả sản phẩm với category và supplier
            var allProducts = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .ToList();

            // Sản phẩm mới: ưu tiên sản phẩm vừa được thêm gần nhất
            var newProducts = allProducts
                .OrderByDescending(p => p.ProductId)
                .Take(8)
                .ToList();

            // Sản phẩm hot: dựa trên số lượng đã order HOẶC flag IsHot
            var hotByOrders = _context.OrderItems
                .GroupBy(oi => oi.ProductId)
                .OrderByDescending(g => g.Sum(oi => oi.Quantity))
                .Take(8)
                .Select(g => g.Key)
                .ToList();

            var hotProducts = allProducts
                .Where(p => hotByOrders.Contains(p.ProductId) || p.IsHot)
                .DistinctBy(p => p.ProductId)
                .OrderByDescending(p => p.IsHot)
                .Take(8)
                .ToList();

            // Sản phẩm deal: flag IsDeal
            var dealProducts = allProducts
                .Where(p => p.IsDeal)
                .Take(8)
                .ToList();

            var categories = _context.Categories.Take(8).ToList();

            ViewBag.NewProducts = newProducts;
            ViewBag.HotProducts = hotProducts;
            ViewBag.DealProducts = dealProducts;
            ViewBag.Categories = categories;

            return View();
        }

        // GET: /Shop/Products - Danh sách sản phẩm
        public IActionResult Products(string? search, int? categoryId, string? sortBy, int page = 1, int pageSize = 12)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(p => (p.Name != null && p.Name.ToLower().Contains(searchLower))
                                       || (p.Description != null && p.Description.ToLower().Contains(searchLower))
                                       || (p.Category != null && p.Category.Name != null && p.Category.Name.ToLower().Contains(searchLower)));
            }

            // Lọc theo danh mục
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // Sắp xếp
            query = sortBy switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_asc" => query.OrderBy(p => p.Name ?? ""),
                "name_desc" => query.OrderByDescending(p => p.Name ?? ""),
                "newest" => query.OrderByDescending(p => p.ProductId),
                _ => query.OrderBy(p => p.Name ?? "")
            };

            var totalItems = query.Count();
            var products = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.SortBy = sortBy;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.Categories = _context.Categories.ToList();

            return View(products);
        }

        // GET: /Shop/Product/{id} - Chi tiết sản phẩm
        public IActionResult Product(int id)
        {
            var product = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Include(p => p.Inventory)
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            // Sản phẩm liên quan
            var relatedProducts = _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != id)
                .Take(4)
                .ToList();

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }

        // GET: /Shop/Cart - Giỏ hàng
        public IActionResult Cart()
        {
            // Lấy giỏ hàng từ session hoặc database
            var cartItems = GetCartItems();
            return View(cartItems);
        }

        // POST: /Shop/AddToCart - Thêm vào giỏ hàng
        // Yêu cầu đăng nhập trước khi thêm vào giỏ hàng
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult AddToCart(int productId, int quantity = 1)
        {
            // Kiểm tra đăng nhập - yêu cầu user phải đăng nhập trước khi thêm vào giỏ
            if (User.Identity?.IsAuthenticated != true)
            {
                // Lưu trạng thái giỏ hàng vào session để khôi phục sau khi đăng nhập
                var pendingAction = new PendingCartAction
                {
                    ProductId = productId,
                    Quantity = quantity,
                    ReturnUrl = Request.Headers.Referer.ToString()
                };
                HttpContext.Session.SetString("PendingCartAction",
                    System.Text.Json.JsonSerializer.Serialize(pendingAction));
                return Json(new
                {
                    success = false,
                    requiresLogin = true,
                    message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng"
                });
            }

            var product = _context.Products
                .Include(p => p.Inventory)
                .FirstOrDefault(p => p.ProductId == productId);

            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại" });
            }

            // Kiểm tra tồn kho trước khi thêm vào giỏ hàng
            var inventory = product.Inventory;
            if (inventory == null || inventory.QuantityInStock < quantity)
            {
                var available = inventory?.QuantityInStock ?? 0;
                return Json(new { success = false, message = $"Không đủ hàng trong kho. Còn {available} sản phẩm." });
            }

            // Thêm vào session cart
            var cart = GetCartItems();
            var existingItem = cart.FirstOrDefault(c => c.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                // Don't store full EF tracked Product into session to avoid circular refs
                cart.Add(new CartItem
                {
                    ProductId = productId,
                    Quantity = quantity,
                    AddedDate = DateTime.Now
                });
            }

            SaveCartItems(cart);

            return Json(new { success = true, message = "Đã thêm vào giỏ hàng", cartCount = cart.Sum(c => c.Quantity) });
        }

        // POST: /Shop/RestorePendingCart - Khôi phục giỏ hàng sau khi đăng nhập
        [HttpPost]
        public IActionResult RestorePendingCart()
        {
            var pendingJson = HttpContext.Session.GetString("PendingCartAction");
            if (string.IsNullOrEmpty(pendingJson))
            {
                return Json(new { success = false, cartCount = GetCartItems().Sum(c => c.Quantity) });
            }

            var pending = System.Text.Json.JsonSerializer.Deserialize<PendingCartAction>(pendingJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (pending == null || pending.ProductId <= 0)
            {
                HttpContext.Session.Remove("PendingCartAction");
                return Json(new { success = false, cartCount = GetCartItems().Sum(c => c.Quantity) });
            }

            var product = _context.Products
                .Include(p => p.Inventory)
                .FirstOrDefault(p => p.ProductId == pending.ProductId);

            if (product != null)
            {
                var qty = pending.Quantity > 0 ? pending.Quantity : 1;
                var inventory = product.Inventory;
                if (inventory == null || inventory.QuantityInStock < qty)
                {
                    qty = inventory?.QuantityInStock ?? 1;
                    if (qty <= 0) qty = 1;
                }

                var cart = GetCartItems();
                var existingItem = cart.FirstOrDefault(c => c.ProductId == pending.ProductId);
                if (existingItem != null)
                    existingItem.Quantity += qty;
                else
                    cart.Add(new CartItem { ProductId = pending.ProductId, Quantity = qty, AddedDate = DateTime.Now });
                SaveCartItems(cart);
            }

            HttpContext.Session.Remove("PendingCartAction");
            return Json(new { success = true, cartCount = GetCartItems().Sum(c => c.Quantity) });
        }

        private class PendingCartAction
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
            public string? ReturnUrl { get; set; }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult UpdateCart(int productId, int quantity)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Remove(item);
                    SaveCartItems(cart);
                    return Json(new { success = true, redirectUrl = Url.Action("Cart") });
                }
                else
                {
                    var product = _context.Products.Include(p => p.Inventory).FirstOrDefault(p => p.ProductId == productId);
                    if (product != null && product.Inventory != null)
                    {
                        if (quantity > product.Inventory.QuantityInStock)
                        {
                            quantity = product.Inventory.QuantityInStock;
                            item.Quantity = quantity;
                            SaveCartItems(cart);
                            return Json(new { success = false, message = $"Số lượng vượt quá tồn kho. Còn lại {quantity} sản phẩm.", newQuantity = quantity });
                        }
                    }

                    item.Quantity = quantity;
                    SaveCartItems(cart);
                }
            }

            return Json(new { success = true, newQuantity = quantity });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);

            if (item != null)
            {
                cart.Remove(item);
                SaveCartItems(cart);
            }

            return Json(new { success = true, redirectUrl = Url.Action("Cart") });
        }

        // GET: /Shop/Checkout - Thanh toán
        // Optional query parameter `selected` contains comma-separated productIds to checkout (e.g. ?selected=1,2)
        public IActionResult Checkout(string? selected)
        {
            var cartItems = GetCartItems();

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

            // If no cart items, just go back to cart
            if (!cartItems.Any())
            {
                return RedirectToAction("Cart");
            }

            // If `selected` provided, filter the cart items to only those selected by the user
            if (!string.IsNullOrWhiteSpace(selected))
            {
                try
                {
                    var ids = selected.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s.Trim(), out var v) ? v : -1)
                        .Where(v => v > 0)
                        .ToHashSet();

                    if (ids.Count > 0)
                    {
                        var filtered = cartItems.Where(ci => ids.Contains(ci.ProductId)).ToList();
                        if (!filtered.Any())
                        {
                            TempData["Error"] = "Không có sản phẩm hợp lệ được chọn để thanh toán.";
                            return RedirectToAction("Cart");
                        }

                        return View(filtered);
                    }
                }
                catch
                {
                    // On parse error, ignore and show full cart as fallback
                }
            }

            // Default: show all cart items
            return View(cartItems);
        }

        // POST: /Shop/PlaceOrder - Đặt hàng
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult PlaceOrder([FromBody] PlaceOrderRequest req)
        {
            if (req == null)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ. Vui lòng nhập đúng định dạng." });
            }

            if (string.IsNullOrWhiteSpace(req.CustomerName) ||
                string.IsNullOrWhiteSpace(req.CustomerPhone) ||
                string.IsNullOrWhiteSpace(req.CustomerAddress))
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin bắt buộc (họ tên, số điện thoại, địa chỉ)." });
            }

            if (!Regex.IsMatch(req.CustomerPhone.Trim(), @"^0\d{9}$"))
            {
                return Json(new { success = false, message = "Số điện thoại không hợp lệ. Vui lòng nhập đúng định dạng (10 chữ số, bắt đầu bằng 0)." });
            }

            // Validate address: at least 10 characters, max 200
            if (req.CustomerAddress.Trim().Length < 10 || req.CustomerAddress.Trim().Length > 200)
            {
                return Json(new { success = false, message = "Địa chỉ giao hàng không hợp lệ. Vui lòng nhập địa chỉ đầy đủ (10-200 ký tự)." });
            }

            // Validate name: at least 3 characters, no special characters
            if (req.CustomerName.Trim().Length < 2 || !Regex.IsMatch(req.CustomerName.Trim(), @"^[\p{L}\s]+$", RegexOptions.Compiled))
            {
                return Json(new { success = false, message = "Họ và tên không hợp lệ. Vui lòng nhập đúng họ tên (ít nhất 2 ký tự, không chứa ký tự đặc biệt)." });
            }

            // Limit Notes length to prevent abuse
            if (req.Notes != null && req.Notes.Length > 500)
            {
                return Json(new { success = false, message = "Ghi chú quá dài (tối đa 500 ký tự)." });
            }

            var cartItems = GetCartItems();

            if (req.SelectedProductIds != null && req.SelectedProductIds.Any())
            {
                cartItems = cartItems.Where(c => req.SelectedProductIds.Contains(c.ProductId)).ToList();
            }

            if (!cartItems.Any())
            {
                return Json(new { success = false, message = "Không có sản phẩm nào để đặt hàng." });
            }

            try
            {
                var order = new Order
                {
                    AccountId = GetCurrentAccountId(),
                    OrderDate = DateTime.Now,
                    Status = "Pending",
                    CustomerName = req.CustomerName.Trim(),
                    CustomerPhone = req.CustomerPhone.Trim(),
                    CustomerAddress = req.CustomerAddress.Trim(),
                    Notes = req.Notes?.Trim(),
                    TotalAmount = cartItems.Sum(c => (c.Product?.Price ?? 0m) * c.Quantity)
                };

                _context.Orders.Add(order);
                _context.SaveChanges();

                foreach (var cartItem in cartItems)
                {
                    var product = _context.Products.Find(cartItem.ProductId);
                    if (product == null) continue;

                    _context.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = Math.Round(product.Price, 2)
                    });
                }

                _context.SaveChanges();

                // KHÔNG clear cart ở đây - chỉ clear khi payment thực sự xác nhận thành công
                // Cart sẽ được clear bởi ConfirmPayment endpoint sau khi user xác nhận đã chuyển khoản
                if (string.Equals(req.PaymentMethod, "vnpay", StringComparison.OrdinalIgnoreCase))
                {
                    var paymentUrl = BuildVnPayPaymentUrl(order);
                    if (string.IsNullOrWhiteSpace(paymentUrl))
                    {
                        paymentUrl = BuildSandboxPaymentUrl(order);
                        if (string.IsNullOrWhiteSpace(paymentUrl))
                        {
                            return Json(new { success = false, message = "Không thể khởi tạo cổng thanh toán. Vui lòng kiểm tra cấu hình VNPAY." });
                        }
                    }

                    return Json(new
                    {
                        success = true,
                        message = "Khởi tạo thanh toán thành công.",
                        orderId = order.OrderId,
                        paymentMethod = "vnpay",
                        requiresRedirect = true,
                        paymentUrl
                    });
                }

                return Json(new { success = true, message = "Đặt hàng thành công!", orderId = order.OrderId });
            }
            catch (Microsoft.Data.SqlClient.SqlException)
            {
                return Json(new { success = false, message = "Thanh toán thất bại do lỗi kết nối cơ sở dữ liệu. Vui lòng thử lại sau." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Thanh toán thất bại. Vui lòng thử lại hoặc liên hệ hỗ trợ." });
            }
        }

        // GET: /Shop/PaymentReturn - Return URL từ VNPAY sau khi user thanh toán xong
        [HttpGet]
        public IActionResult PaymentReturn()
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

            var order = _context.Orders.FirstOrDefault(o => o.OrderId == orderId);
            if (order == null)
            {
                TempData["Error"] = "Đơn hàng không tồn tại.";
                return RedirectToAction("Checkout");
            }

            var responseCode = data.GetValueOrDefault("vnp_ResponseCode");
            var transactionStatus = data.GetValueOrDefault("vnp_TransactionStatus");
            var isSuccess = responseCode == "00" && transactionStatus == "00";

            if (isSuccess && order.Status != "Confirmed")
            {
                order.Status = "Confirmed";
                _context.SaveChanges();
            }

            if (isSuccess && User.Identity?.IsAuthenticated == true && GetCurrentAccountId() == order.AccountId)
            {
                ClearCart();
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
        public IActionResult PaymentIpn()
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

            var order = _context.Orders.FirstOrDefault(o => o.OrderId == orderId);
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
                order.Status = "Confirmed";
                _context.SaveChanges();
            }

            return Json(new { RspCode = "00", Message = "Confirm Success" });
        }

        // GET: /Shop/SandboxAutoPay - Giả lập thanh toán tự động cho môi trường demo/sinh viên
        [HttpGet]
        public IActionResult SandboxAutoPay(int orderId)
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
            var order = _context.Orders.FirstOrDefault(o => o.OrderId == orderId && o.AccountId == accountId);
            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng cần thanh toán.";
                return RedirectToAction("OrderHistory");
            }

            if (order.Status != "Confirmed")
            {
                order.Status = "Confirmed";
                _context.SaveChanges();

                ClearCart();
                TempData["Success"] = "Demo: hệ thống đã tự động xác nhận thanh toán thành công.";
            }

            return RedirectToAction("OrderDetail", new { id = orderId, payment = "success" });
        }

        // POST: /Shop/ConfirmPayment - Xác nhận thanh toán thực sự (clear cart sau khi user xác nhận đã chuyển khoản)
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult ConfirmPayment(int orderId)
        {
            if (orderId <= 0)
            {
                return Json(new { success = false, message = "Mã đơn hàng không hợp lệ." });
            }

            var accountId = GetCurrentAccountId();
            var order = _context.Orders.FirstOrDefault(o => o.OrderId == orderId && o.AccountId == accountId);
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng hoặc đơn hàng không thuộc về bạn." });
            }

            // Prevent double confirmation
            if (order.Status == "Confirmed")
            {
                return Json(new { success = false, message = "Đơn hàng đã được xác nhận thanh toán trước đó." });
            }

            try
            {
                // Cập nhật trạng thái đơn hàng
                order.Status = "Confirmed";
                _context.SaveChanges();

                // Clear items that were purchased from cart
                var orderedProductIds = _context.OrderItems.Where(oi => oi.OrderId == orderId).Select(oi => oi.ProductId).ToList();
                var cartItems = GetCartItems();
                cartItems.RemoveAll(c => orderedProductIds.Contains(c.ProductId));
                if (cartItems.Any())
                {
                    SaveCartItems(cartItems);
                }
                else
                {
                    ClearCart();
                }

                return Json(new { success = true, message = "Xác nhận thanh toán thành công!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xác nhận thanh toán." });
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

        // GET: /Shop/GetCartCount - Lấy số lượng giỏ hàng
        public IActionResult GetCartCount()
        {
            var cartItems = GetCartItems();
            return Json(cartItems.Sum(c => c.Quantity));
        }

        // Helper methods
        private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };

        private int GetCurrentAccountId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(claim) && int.TryParse(claim, out var accountId))
            {
                return accountId;
            }
            return 1;
        }

        private List<CartItem> GetCartItems()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<CartItem>();
            }

            var cartItems = System.Text.Json.JsonSerializer.Deserialize<List<CartItem>>(cartJson, JsonOptions);
            if (cartItems == null) return new List<CartItem>();

            // Load product details (don't keep EF tracked children in session)
            foreach (var item in cartItems)
            {
                var product = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .FirstOrDefault(p => p.ProductId == item.ProductId);
                item.Product = product;
            }

            return cartItems.Where(ci => ci.Product != null).ToList();
        }

        private void SaveCartItems(List<CartItem> cartItems)
        {
            // Create a lightweight copy without navigation properties to avoid cycles and large payloads
            var lightweight = cartItems.Select(ci => new CartItem
            {
                CartItemId = ci.CartItemId,
                AccountId = ci.AccountId,
                ProductId = ci.ProductId,
                Quantity = ci.Quantity,
                AddedDate = ci.AddedDate
            }).ToList();

            var cartJson = System.Text.Json.JsonSerializer.Serialize(lightweight, JsonOptions);
            HttpContext.Session.SetString("Cart", cartJson);
        }

        private void ClearCart()
        {
            HttpContext.Session.Remove("Cart");
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

        private void RemoveOrderedItemsFromSessionCart(int orderId)
        {
            var orderedProductIds = _context.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .Select(oi => oi.ProductId)
                .ToList();

            if (!orderedProductIds.Any())
            {
                return;
            }

            var cartItems = GetCartItems();
            cartItems.RemoveAll(c => orderedProductIds.Contains(c.ProductId));

            if (cartItems.Any())
            {
                SaveCartItems(cartItems);
            }
            else
            {
                ClearCart();
            }
        }

        // GET: /Shop/Support - Trang hỗ trợ
        public IActionResult Support()
        {
            return View();
        }

        // GET: /Shop/Search - Tìm kiếm realtime (AJAX)
        public IActionResult Search(string? q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return Json(new List<object>());
            }

            var results = _context.Products
                .Include(p => p.Category)
                .Where(p => p.Name.ToLower().Contains(q.ToLower())
                         || (p.Description != null && p.Description.ToLower().Contains(q.ToLower())))
                .Take(8)
                .Select(p => new
                {
                    p.ProductId,
                    p.Name,
                    p.Price,
                    p.ImageUrl,
                    CategoryName = p.Category != null ? p.Category.Name : ""
                })
                .ToList();

            return Json(results);
        }

        // GET: /Shop/OrderHistory - Lịch sử đơn hàng
        public IActionResult OrderHistory()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Auth");
            }

            var accountId = GetCurrentAccountId();
            var orders = _context.Orders
                .Where(o => o.AccountId == accountId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        // GET: /Shop/OrderDetail/{id} - Chi tiết đơn hàng
        public IActionResult OrderDetail(int id, string? payment = null)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Auth");
            }

            var accountId = GetCurrentAccountId();
            var order = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefault(o => o.OrderId == id && o.AccountId == accountId);

            if (order == null)
            {
                return NotFound();
            }

            ViewBag.PaymentResult = payment;

            return View(order);
        }

        // GET: /Shop/Profile - Thông tin cá nhân
        public IActionResult Profile()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Auth");
            }

            var accountId = GetCurrentAccountId();
            var account = _context.Accounts.Find(accountId);
            if (account == null)
            {
                return NotFound();
            }

            return View(account);
        }

        // POST: /Shop/UpdateProfile - Cập nhật thông tin cá nhân
        [HttpPost]
        public IActionResult UpdateProfile(string fullName, string email, string phone, string address)
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

            var account = _context.Accounts.Find(accountId);
            if (account == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản." });
            }

            try
            {
                account.FullName = fullName.Trim();
                account.Email = email?.Trim();
                account.Phone = phone.Trim();
                account.Address = address?.Trim();
                _context.SaveChanges();
                return Json(new { success = true, message = "Cập nhật thông tin thành công!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Cập nhật thất bại. Vui lòng thử lại." });
            }
        }
    }
}

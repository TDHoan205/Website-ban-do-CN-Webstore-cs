using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Webstore.Data;
using Webstore.Models;

namespace Webstore.Controllers
{
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShopController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Shop - Trang chủ shop
        public IActionResult Index()
        {
            // Lấy tất cả sản phẩm với category và supplier
            var allProducts = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .ToList();

            // Sản phẩm mới: top 8 sản phẩm mới nhất (ProductId cao nhất)
            var maxProductId = allProducts.Any() ? allProducts.Max(p => p.ProductId) : 0;
            var newProducts = allProducts
                .Where(p => p.ProductId > maxProductId - 8 || p.IsNew)
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
                return Json(new { success = false, requiresLogin = true,
                    message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng" });
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
        public IActionResult UpdateCart(int productId, int quantity)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }
                SaveCartItems(cart);
            }

            return Json(new { success = true, redirectUrl = Url.Action("Cart") });
        }

        [HttpPost]
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
                BankName = paymentSection?["BankName"] ?? "Vietcombank",
                AccountNumber = paymentSection?["AccountNumber"] ?? "123456789012",
                AccountName = paymentSection?["AccountName"] ?? "WEBSTORE SHOP"
            };

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
            if (!cartItems.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng trống. Vui lòng thêm sản phẩm trước khi đặt hàng." });
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

        // POST: /Shop/ConfirmPayment - Xác nhận thanh toán thực sự (clear cart sau khi user xác nhận đã chuyển khoản)
        [HttpPost]
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

                // Clear cart sau khi thanh toán thực sự được xác nhận
                ClearCart();

                return Json(new { success = true, message = "Xác nhận thanh toán thành công!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xác nhận thanh toán." });
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
        public IActionResult OrderDetail(int id)
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

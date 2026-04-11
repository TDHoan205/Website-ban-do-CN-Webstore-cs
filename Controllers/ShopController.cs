using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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
            // Sản phẩm nổi bật (mới nhất)
            var featuredProducts = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .OrderByDescending(p => p.ProductId)
                .Take(8)
                .ToList();

            // Sản phẩm hot (bán chạy nhất dựa trên số lượng đã order)
            var hotProducts = _context.OrderItems
                .GroupBy(oi => oi.ProductId)
                .OrderByDescending(g => g.Sum(oi => oi.Quantity))
                .Take(8)
                .Select(g => g.Key)
                .ToList();

            var productsHot = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Where(p => hotProducts.Contains(p.ProductId))
                .ToList();

            // Sắp xếp theo thứ tự hot
            productsHot = productsHot
                .OrderBy(p => hotProducts.IndexOf(p.ProductId))
                .ToList();

            // Nếu chưa có đơn hàng nào, lấy sản phẩm ngẫu nhiên làm hot
            if (!productsHot.Any())
            {
                productsHot = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .OrderByDescending(p => p.ProductId)
                    .Take(8)
                    .ToList();
            }

            var categories = _context.Categories.Take(6).ToList();

            ViewBag.FeaturedProducts = featuredProducts;
            ViewBag.HotProducts = productsHot;
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
        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity = 1)
        {
            var product = _context.Products
                .Include(p => p.Inventory)
                .FirstOrDefault(p => p.ProductId == productId);

            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại" });
            }

            // Kiểm tra tồn kho (tạm thời bỏ qua)
            // var inventory = product.Inventory.FirstOrDefault();
            // if (inventory == null || inventory.Quantity < quantity)
            // {
            //     return Json(new { success = false, message = "Không đủ hàng trong kho" });
            // }

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

        // POST: /Shop/UpdateCart - Cập nhật giỏ hàng
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

            return RedirectToAction("Cart");
        }

        // POST: /Shop/RemoveFromCart - Xóa khỏi giỏ hàng
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

            return RedirectToAction("Cart");
        }

        // GET: /Shop/Checkout - Thanh toán
        // Optional query parameter `selected` contains comma-separated productIds to checkout (e.g. ?selected=1,2)
        public IActionResult Checkout(string? selected)
        {
            var cartItems = GetCartItems();

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
        public IActionResult PlaceOrder(string customerName, string customerPhone, string customerAddress, string notes = "")
        {
            var cartItems = GetCartItems();
            if (!cartItems.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng trống" });
            }

            // Tạo đơn hàng
            // Lấy account id từ user đã đăng nhập nếu có, ngược lại giữ mặc định 1
            int accountId = 1;
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(claim) && int.TryParse(claim, out var parsedAccountId))
            {
                accountId = parsedAccountId;
            }

            var order = new Order
            {
                AccountId = accountId,
                OrderDate = DateTime.Now,
                Status = "Pending",
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                CustomerAddress = customerAddress,
                Notes = notes,
                TotalAmount = cartItems.Sum(c => (c.Product?.Price ?? 0m) * c.Quantity)
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            // Tạo chi tiết đơn hàng (kiểm tra trước để tránh lỗi FK/constraint)
            var itemsAdded = 0;
            foreach (var cartItem in cartItems)
            {
                // Reload product from DB to ensure it exists and get up-to-date price
                var product = _context.Products.Find(cartItem.ProductId);
                if (product == null)
                {
                    // Log missing product and skip this cart item
                    try { Console.Error.WriteLine($"[ShopController] Warning: Product {cartItem.ProductId} not found while creating order {order.OrderId}"); } catch { }
                    continue;
                }

                var orderItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = Math.Round(product.Price, 2)
                };
                _context.OrderItems.Add(orderItem);
                itemsAdded++;
            }

            if (itemsAdded > 0)
            {
                try
                {
                    _context.SaveChanges();
                }
                catch (Exception ex)
                {
                    // Log full exception for diagnosis but still return success to user
                    try { Console.Error.WriteLine($"[ShopController] Error saving order items for order {order.OrderId}: {ex}"); } catch { }
                }
            }
            else
            {
                try { Console.Error.WriteLine($"[ShopController] Warning: No order items were added for order {order.OrderId}"); } catch { }
            }

            // Xóa giỏ hàng
            ClearCart();

            return Json(new { success = true, message = "Đặt hàng thành công!", orderId = order.OrderId });
        }

        // GET: /Shop/GetCartCount - Lấy số lượng giỏ hàng
        public IActionResult GetCartCount()
        {
            var cartItems = GetCartItems();
            return Json(cartItems.Sum(c => c.Quantity));
        }

        // Helper methods
        private List<CartItem> GetCartItems()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<CartItem>();
            }

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            };

            var cartItems = System.Text.Json.JsonSerializer.Deserialize<List<CartItem>>(cartJson, options);
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
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            };

            // Create a lightweight copy without navigation properties to avoid cycles and large payloads
            var lightweight = cartItems.Select(ci => new CartItem
            {
                CartItemId = ci.CartItemId,
                AccountId = ci.AccountId,
                ProductId = ci.ProductId,
                Quantity = ci.Quantity,
                AddedDate = ci.AddedDate
            }).ToList();

            var cartJson = System.Text.Json.JsonSerializer.Serialize(lightweight, options);
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
    }
}

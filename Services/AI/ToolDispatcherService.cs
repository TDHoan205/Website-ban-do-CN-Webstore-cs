using System.Text.Json;
using System.Text.RegularExpressions;
using Webstore.Services;

namespace Webstore.Services.AI
{
    public interface IToolDispatcherService
    {
        List<GeminiTool> GetAvailableTools(string role);
        Task<ToolExecutionResult> DispatchAsync(string userMessage, string aiResponse, string role);
    }

    public class ToolExecutionResult
    {
        public string ResponseMessage { get; set; } = "";
        public object? Data { get; set; }
    }

    /// <summary>
    /// Tool cho Gemini - định nghĩa function calls có thể gọi
    /// </summary>
    public class GeminiTool
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string ParametersSchema { get; set; } = "{}";
    }

    /// <summary>
    /// Gemini Tool Dispatcher - Xử lý function calling cho Gemini
    /// Vì Gemini không hỗ trợ function calling native như OpenAI,
    /// chúng ta sẽ parse AI response để tìm function calls
    /// </summary>
    public class GeminiToolDispatcherService : IToolDispatcherService
    {
        private readonly ICartService _cartService;
        private readonly IInventoryService _inventoryService;
        private readonly IProductService _productService;

        public GeminiToolDispatcherService(
            ICartService cartService,
            IInventoryService inventoryService,
            IProductService productService)
        {
            _cartService = cartService;
            _inventoryService = inventoryService;
            _productService = productService;
        }

        public List<GeminiTool> GetAvailableTools(string role)
        {
            var tools = new List<GeminiTool>();

            if (role == "customer" || role == "user")
            {
                tools.Add(new GeminiTool
                {
                    Name = "add_to_cart",
                    Description = "Thêm sản phẩm vào giỏ hàng. Gọi khi khách hàng muốn mua hoặc đặt hàng.",
                    ParametersSchema = @"{
                        ""productId"": { ""type"": ""integer"", ""description"": ""ID của sản phẩm"" },
                        ""quantity"": { ""type"": ""integer"", ""description"": ""Số lượng (mặc định là 1)"" }
                    }"
                });

                tools.Add(new GeminiTool
                {
                    Name = "check_inventory",
                    Description = "Kiểm tra xem sản phẩm còn hàng hay không.",
                    ParametersSchema = @"{
                        ""productId"": { ""type"": ""integer"", ""description"": ""ID của sản phẩm"" }
                    }"
                });

                tools.Add(new GeminiTool
                {
                    Name = "search_products",
                    Description = "Tìm kiếm sản phẩm theo từ khóa.",
                    ParametersSchema = @"{
                        ""query"": { ""type"": ""string"", ""description"": ""Từ khóa tìm kiếm"" }
                    }"
                });
            }

            if (role == "admin")
            {
                tools.Add(new GeminiTool
                {
                    Name = "get_inventory_report",
                    Description = "Lấy báo cáo tồn kho.",
                    ParametersSchema = @"{}"
                });

                tools.Add(new GeminiTool
                {
                    Name = "get_sales_stats",
                    Description = "Lấy thống kê doanh thu.",
                    ParametersSchema = @"{
                        ""period"": { ""type"": ""string"", ""description"": ""day|week|month|year"" }
                    }"
                });
            }

            return tools;
        }

        public async Task<ToolExecutionResult> DispatchAsync(string userMessage, string aiResponse, string role)
        {
            var result = new ToolExecutionResult();

            // Parse potential function calls from AI response
            var functionCalls = ParseFunctionCalls(aiResponse);

            if (!functionCalls.Any())
            {
                result.ResponseMessage = aiResponse;
                return result;
            }

            var messages = new List<string>();

            foreach (var call in functionCalls)
            {
                var execResult = await ExecuteFunctionAsync(call.FunctionName, call.Arguments);
                if (!string.IsNullOrEmpty(execResult))
                {
                    messages.Add(execResult);
                }
            }

            result.ResponseMessage = string.Join("\n", messages);
            return result;
        }

        private List<FunctionCall> ParseFunctionCalls(string response)
        {
            var calls = new List<FunctionCall>();

            // Pattern 1: JSON format
            var jsonPattern = @"\{\s*""function""\s*:\s*""(\w+)"".*?""args""\s*:\s*(\{[^}]+\})";
            var jsonMatches = Regex.Matches(response, jsonPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            foreach (Match match in jsonMatches)
            {
                calls.Add(new FunctionCall
                {
                    FunctionName = match.Groups[1].Value,
                    Arguments = match.Groups[2].Value
                });
            }

            // Pattern 2: Natural language extraction
            var addToCartPattern = @"(?:thêm|add).*?(?:vào\s*giỏ|sản\s*phẩm|product).*?(?:id[:\s]*)?(\d+)";
            var cartMatches = Regex.Matches(response, addToCartPattern, RegexOptions.IgnoreCase);

            foreach (Match match in cartMatches)
            {
                if (match.Groups.Count > 1)
                {
                    var productId = match.Groups[1].Value;
                    calls.Add(new FunctionCall
                    {
                        FunctionName = "add_to_cart",
                        Arguments = $"{{\"productId\": {productId}}}"
                    });
                }
            }

            // Pattern 3: Check inventory
            var checkInventoryPattern = @"(?:kiểm\s*tra|còn\s*hàng|stock|inventory).*?(?:id[:\s]*)?(\d+)";
            var inventoryMatches = Regex.Matches(response, checkInventoryPattern, RegexOptions.IgnoreCase);

            foreach (Match match in inventoryMatches)
            {
                if (match.Groups.Count > 1)
                {
                    var productId = match.Groups[1].Value;
                    calls.Add(new FunctionCall
                    {
                        FunctionName = "check_inventory",
                        Arguments = $"{{\"productId\": {productId}}}"
                    });
                }
            }

            return calls;
        }

        private async Task<string> ExecuteFunctionAsync(string functionName, string arguments)
        {
            try
            {
                var args = JsonDocument.Parse(arguments);
                var root = args.RootElement;

                switch (functionName.ToLower())
                {
                    case "add_to_cart":
                        return await ExecuteAddToCartAsync(root);

                    case "check_inventory":
                        return await ExecuteCheckInventoryAsync(root);

                    case "search_products":
                        return await ExecuteSearchProductsAsync(root);

                    default:
                        return $"[Unknown function: {functionName}]";
                }
            }
            catch (Exception ex)
            {
                return $"[Error executing {functionName}: {ex.Message}]";
            }
        }

        private async Task<string> ExecuteAddToCartAsync(JsonElement args)
        {
            int productId = 0;
            int quantity = 1;

            if (args.TryGetProperty("productId", out var productIdElement))
            {
                productId = productIdElement.GetInt32();
            }

            if (args.TryGetProperty("quantity", out var qtyElement))
            {
                quantity = qtyElement.GetInt32();
            }

            if (productId == 0)
            {
                return "Không tìm thấy ID sản phẩm để thêm vào giỏ.";
            }

            var product = await _productService.GetProductByIdAsync(productId);
            if (product != null)
            {
                await _cartService.AddToCartAsync(productId, null, quantity);
                return $"Đã thêm {quantity} x {product.Name} vào giỏ hàng!";
            }

            return $"Không tìm thấy sản phẩm với ID {productId}.";
        }

        private async Task<string> ExecuteCheckInventoryAsync(JsonElement args)
        {
            int productId = 0;

            if (args.TryGetProperty("productId", out var productIdElement))
            {
                productId = productIdElement.GetInt32();
            }

            if (productId == 0)
            {
                return "Không có ID sản phẩm để kiểm tra.";
            }

            var inventory = await _inventoryService.GetByProductIdAsync(productId);
            if (inventory != null)
            {
                return inventory.StockQuantity > 0
                    ? $"Sản phẩm còn {inventory.StockQuantity} máy trong kho."
                    : "Sản phẩm hiện đang tạm hết hàng.";
            }

            return "Không có thông tin tồn kho cho sản phẩm này.";
        }

        private async Task<string> ExecuteSearchProductsAsync(JsonElement args)
        {
            string query = "";

            if (args.TryGetProperty("query", out var queryElement))
            {
                query = queryElement.GetString() ?? "";
            }

            if (string.IsNullOrEmpty(query))
            {
                return "Vui lòng cung cấp từ khóa tìm kiếm.";
            }

            var productsData = await _productService.SearchRealtime(query, 5);
            if (productsData.Any())
            {
                var result = $"Tìm thấy {productsData.Count()} sản phẩm:\n";
                foreach (var p in productsData.Take(5))
                {
                    // productsData is IEnumerable<object> with anonymous types
                    result += $"- {p}\n";
                }
                return result;
            }

            return "Không tìm thấy sản phẩm nào phù hợp.";
        }

        private class FunctionCall
        {
            public string FunctionName { get; set; } = "";
            public string Arguments { get; set; } = "";
        }
    }

    /// <summary>
    /// Legacy adapter để giữ compatibility với code cũ
    /// </summary>
    public class ToolDispatcherService : IToolDispatcherService
    {
        private readonly ICartService _cartService;
        private readonly IInventoryService _inventoryService;
        private readonly IProductService _productService;

        public ToolDispatcherService(
            ICartService cartService,
            IInventoryService inventoryService,
            IProductService productService)
        {
            _cartService = cartService;
            _inventoryService = inventoryService;
            _productService = productService;
        }

        public List<GeminiTool> GetAvailableTools(string role)
        {
            var tools = new List<GeminiTool>();

            if (role == "customer")
            {
                tools.Add(new GeminiTool
                {
                    Name = "add_to_cart",
                    Description = "Thêm sản phẩm vào giỏ hàng. Gọi khi khách hàng muốn mua hoặc đặt hàng.",
                    ParametersSchema = @"{
                        ""productId"": { ""type"": ""integer"", ""description"": ""ID của sản phẩm"" },
                        ""quantity"": { ""type"": ""integer"", ""description"": ""Số lượng (mặc định là 1)"" }
                    }"
                });

                tools.Add(new GeminiTool
                {
                    Name = "check_inventory",
                    Description = "Kiểm tra xem sản phẩm còn hàng hay không.",
                    ParametersSchema = @"{
                        ""productId"": { ""type"": ""integer"", ""description"": ""ID của sản phẩm"" }
                    }"
                });
            }

            return tools;
        }

        public async Task<ToolExecutionResult> DispatchAsync(string userMessage, string aiResponse, string role)
        {
            var dispatcher = new GeminiToolDispatcherService(_cartService, _inventoryService, _productService);
            return await dispatcher.DispatchAsync(userMessage, aiResponse, role);
        }
    }
}

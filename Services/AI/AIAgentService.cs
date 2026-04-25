using System.Text.Json;
using Microsoft.Extensions.Logging;
using Webstore.Models.AI;

namespace Webstore.Services.AI
{
    public interface IAIAgentService
    {
        Task<AIResponse> ProcessMessageAsync(string userMessage, string role = "customer", Guid? sessionId = null);
    }

    /// <summary>
    /// AI Agent Service - Điều phối AI response cho chatbot
    /// Sử dụng Gemini thay vì OpenAI, với fallback rule-based
    /// </summary>
    public class AIAgentService : IAIAgentService
    {
        private readonly IGeminiService _gemini;
        private readonly RAGEngineService _ragEngine;
        private readonly IToolDispatcherService _dispatcher;
        private readonly ILogger<AIAgentService>? _logger;

        public AIAgentService(
            IGeminiService gemini,
            RAGEngineService ragEngine,
            IToolDispatcherService dispatcher,
            ILogger<AIAgentService>? logger = null)
        {
            _gemini = gemini;
            _ragEngine = ragEngine;
            _dispatcher = dispatcher;
            _logger = logger;
        }

        public async Task<AIResponse> ProcessMessageAsync(string userMessage, string role = "customer", Guid? sessionId = null)
        {
            // 1. Get RAG Context
            var context = await _ragEngine.GetContextAsync(userMessage);

            // 2. Try Gemini API first
            string? finalMessage = null;
            bool geminiSuccess = false;

            try
            {
                // Prepare system prompt based on role
                string systemPrompt = role switch
                {
                    "admin" => GetAdminSystemPrompt(context),
                    _ => GetCustomerSystemPrompt(context)
                };

                // Call Gemini API
                finalMessage = await _gemini.GetChatResponseAsync(systemPrompt, userMessage);
                geminiSuccess = true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Gemini API failed, using rule-based fallback");
            }

            // 3. If Gemini failed or returned empty, use rule-based fallback
            if (!geminiSuccess || string.IsNullOrWhiteSpace(finalMessage))
            {
                return GenerateFallbackResponse(userMessage, context, role);
            }

            // 4. Try to dispatch function calls if detected
            if (tools.Any())
            {
                var toolResult = await _dispatcher.DispatchAsync(userMessage, finalMessage, role);
                if (!string.IsNullOrEmpty(toolResult.ResponseMessage) &&
                    toolResult.ResponseMessage != finalMessage)
                {
                    finalMessage = $"{finalMessage}\n\n{toolResult.ResponseMessage}";
                }
            }

            return new AIResponse
            {
                Message = finalMessage,
                Intent = context.Intent,
                Confidence = context.Confidence,
                Products = context.Products,
                Faqs = context.FAQs,
                ShouldEscalate = context.ShouldEscalate
            };
        }

        private static readonly List<(string[] Keywords, string Intent)> IntentPatterns = new()
        {
            (new[] { "xin chào", "chào", "hi", "hello", "hey" }, "greeting"),
            (new[] { "cảm ơn", "thanks", "thank" }, "thanks"),
            (new[] { "mua", "cần", "tìm", "muốn", "đặt" }, "purchase"),
            (new[] { "hỏi", "thông tin", "biết", "xem" }, "inquiry"),
            (new[] { "bảo hành", "hỏng", "lỗi", "sửa", "đổi", "trả" }, "warranty"),
            (new[] { "thanh toán", "chuyển khoản", "cod", "vnpay" }, "payment"),
            (new[] { "giao", "ship", "vận chuyển", "nhận hàng" }, "shipping"),
            (new[] { "đơn hàng", "theo dõi", "giao chưa", "tình trạng" }, "order_status"),
            (new[] { "phàn nàn", "không hài lòng", "khiếu nại" }, "complaint")
        };

        private AIResponse GenerateFallbackResponse(string userMessage, RAGContext context, string role)
        {
            var lowerMsg = userMessage.ToLower();

            // Detect intent
            var intent = "general";
            foreach (var (keywords, detectedIntent) in IntentPatterns)
            {
                if (keywords.Any(k => lowerMsg.Contains(k)))
                {
                    intent = detectedIntent;
                    break;
                }
            }

            // Generate response based on intent
            var response = intent switch
            {
                "greeting" => GenerateGreeting(),
                "thanks" => "Cảm ơn bạn! Rất vui được hỗ trợ bạn. Nếu cần gì thêm, mình sẵn sàng giúp nhé! 😊",

                "purchase" => GeneratePurchaseResponse(context),
                "inquiry" => GenerateInquiryResponse(context),
                "warranty" => "TechStore hỗ trợ bảo hành chính hãng cho tất cả sản phẩm:\n\n" +
                    "• Điện thoại: Bảo hành 12-24 tháng\n" +
                    "• Laptop: Bảo hành 12-36 tháng\n" +
                    "• Phụ kiện: Bảo hành 6-12 tháng\n\n" +
                    "Bạn có câu hỏi cụ thể nào về bảo hành không?",

                "payment" => "TechStore hỗ trợ nhiều phương thức thanh toán:\n\n" +
                    "• 💵 COD (nhận hàng rồi trả tiền)\n" +
                    "• 🏦 Chuyển khoản ngân hàng\n" +
                    "• 💳 Thẻ Visa/Mastercard\n" +
                    "• 📱 Ví điện tử: VNPay, MoMo, ZaloPay\n\n" +
                    "Thanh toán online được bảo mật qua cổng VNPay. Bạn yên tâm nhé!",

                "shipping" => "Thông tin giao hàng tại TechStore:\n\n" +
                    "• 🚚 Nội thành: Giao trong 1-2 ngày\n" +
                    "• 🚛 Ngoại thành/Tỉnh: Giao trong 3-5 ngày\n" +
                    "• 📦 Miễn phí ship cho đơn từ 500.000đ\n" +
                    "• 💰 Dưới 500.000đ: Phí 30.000đ\n\n" +
                    "Bạn ở khu vực nào để mình tư vấn chính xác hơn?",

                "order_status" => "Để kiểm tra trạng thái đơn hàng:\n\n" +
                    "1️⃣ Đăng nhập vào tài khoản\n" +
                    "2️⃣ Vào mục \"Đơn hàng của tôi\"\n" +
                    "3️⃣ Chọn đơn hàng cần kiểm tra\n\n" +
                    "Tại đây bạn sẽ thấy: Đang xử lý → Đã xác nhận → Đang giao → Đã giao",

                "complaint" => "Mình rất tiếc khi bạn không hài lòng. 😔\n\n" +
                    "TechStore luôn lắng nghe khách hàng. Bạn có thể:\n" +
                    "• Mô tả chi tiết vấn đề để mình hỗ trợ\n" +
                    "• Liên hệ hotline: 1900.xxxx\n" +
                    "• Hoặc bấm \"Gặp quản trị viên\" để được hỗ trợ trực tiếp",

                _ => GenerateGeneralResponse(context)
            };

            return new AIResponse
            {
                Message = response,
                Intent = intent,
                Confidence = 0.7m,
                ShouldEscalate = intent == "complaint",
                Products = context.Products,
                Faqs = context.FAQs
            };
        }

        private static string GenerateGreeting()
        {
            var greetings = new[]
            {
                "Xin chào! 👋 Mình là trợ lý TechStore. Mình có thể giúp bạn tìm sản phẩm công nghệ, tư vấn bảo hành, thanh toán hay giao hàng.",
                "Chào bạn! Rất vui được hỗ trợ bạn tại TechStore. Bạn đang quan tâm đến sản phẩm nào hôm nay?",
                "Hi! Mình là trợ lý AI của TechStore. Mình sẵn sàng giúp bạn tìm kiếm sản phẩm, tư vấn mua hàng, hay giải đáp thắc mắc."
            };
            return greetings[DateTime.Now.Second % greetings.Length];
        }

        private static string GeneratePurchaseResponse(RAGContext context)
        {
            var response = "Bạn đang tìm mua sản phẩm công nghệ phải không? ";

            if (context.Products.Count > 0)
            {
                response += "Mình gợi ý một số sản phẩm phù hợp:\n\n";

                foreach (var product in context.Products.Take(3))
                {
                    response += $"📱 {product.Name}\n";
                    response += $"   💰 Giá: {product.Price:N0}đ\n";
                    response += $"   ✨ {BuildValueDescription(product.Specs, product.Price)}\n\n";
                }

                response += "Bạn có muốn xem chi tiết sản phẩm nào không?";
            }
            else
            {
                response += "Bạn có thể cho mình biết thêm về nhu cầu và ngân sách để mình tư vấn tốt hơn nhé!";
            }

            return response;
        }

        private static string GenerateInquiryResponse(RAGContext context)
        {
            if (context.FAQs.Count > 0)
            {
                var response = "Mình tìm thấy một số thông tin liên quan:\n\n";
                foreach (var faq in context.FAQs.Take(3))
                {
                    response += $"❓ {faq.Question}\n";
                    response += $"💡 {faq.Answer}\n\n";
                }
                return response;
            }

            return "Bạn có thể hỏi mình về:\n\n" +
                "• Thông tin sản phẩm (giá, cấu hình)\n" +
                "• Bảo hành và đổi trả\n" +
                "• Phương thức thanh toán\n" +
                "• Giao hàng và vận chuyển\n\n" +
                "Bạn đang quan tâm đến vấn đề gì?";
        }

        private static string GenerateGeneralResponse(RAGContext context)
        {
            if (context.Products.Count > 0)
            {
                var response = "Mình có một số gợi ý cho bạn:\n\n";

                foreach (var product in context.Products.Take(3))
                {
                    response += $"📱 {product.Name}\n";
                    response += $"   Giá: {product.Price:N0}đ\n\n";
                }

                response += "Bạn có muốn tìm hiểu thêm về sản phẩm nào không?";
                return response;
            }

            return "Cảm ơn bạn đã nhắn tin! 👋\n\n" +
                "Mình có thể hỗ trợ bạn về:\n" +
                "• Tìm kiếm và tư vấn sản phẩm công nghệ\n" +
                "• Thông tin bảo hành, đổi trả\n" +
                "• Phương thức thanh toán\n" +
                "• Tình trạng giao hàng\n\n" +
                "Bạn cần mình giúp gì hôm nay?";
        }

        private static string BuildValueDescription(string? specs, decimal price)
        {
            if (string.IsNullOrWhiteSpace(specs))
            {
                return "Hiệu năng ổn định, trải nghiệm dễ dùng, mức giá hợp lý.";
            }

            var text = specs.ToLowerInvariant();

            if (text.Contains("snapdragon") || text.Contains("ryzen") || text.Contains("intel core") || text.Contains("apple m") || text.Contains("a17"))
            {
                return "Hiệu năng mạnh, xử lý tốt đa nhiệm và ứng dụng nặng.";
            }

            if (text.Contains("120hz") || text.Contains("oled") || text.Contains("amoled"))
            {
                return "Màn hình đẹp, chuyển động mượt, trải nghiệm thị giác tốt.";
            }

            if (text.Contains("5000mah") || text.Contains("5500mah"))
            {
                return "Pin bền và sạc nhanh, phù hợp dùng liên tục cả ngày.";
            }

            return price switch
            {
                < 8_000_000 => "Giá trị kinh tế cao trong tầm giá phổ thông.",
                < 20_000_000 => "Giá trị kinh tế tốt, cân bằng giữa cấu hình và chi phí.",
                _ => "Giá trị dài hạn cho nhu cầu hiệu năng và trải nghiệm cao cấp."
            };
        }

        private IEnumerable<GeminiTool> tools => _dispatcher.GetAvailableTools("customer");

        private string GetCustomerSystemPrompt(RAGContext context)
        {
            var productsInfo = "";
            if (context.Products.Any())
            {
                productsInfo = "\n\nSản phẩm có sẵn trong database:\n";
                foreach (var p in context.Products.Take(5))
                {
                    productsInfo += $"- {p.Name} (ID: {p.ProductId}) - Giá: {p.Price:N0}đ\n";
                }
            }

            var faqInfo = "";
            if (context.FAQs.Any())
            {
                faqInfo = "\n\nFAQ có sẵn:\n";
                foreach (var f in context.FAQs.Take(3))
                {
                    faqInfo += $"- Q: {f.Question}\n  A: {f.Answer}\n";
                }
            }

            return $@"Bạn là trợ lý ảo thân thiện của TechStore - cửa hàng công nghệ hàng đầu Việt Nam.
Nhiệm vụ của bạn:
1. Tư vấn sản phẩm công nghệ (điện thoại, laptop, tablet, phụ kiện)
2. Trả lời câu hỏi về bảo hành, thanh toán, giao hàng
3. Hỗ trợ khách hàng một cách chuyên nghiệp và thân thiện

Nguyên tắc:
- Trả lời bằng tiếng Việt
- Nếu khách muốn mua hàng, hãy đề cập đến sản phẩm phù hợp và hướng dẫn họ thêm vào giỏ
- Nếu không biết câu trả lời, hãy khuyên khách liên hệ hotline hoặc gặp admin
- Đưa ra lời khuyên hữu ích dựa trên nhu cầu và ngân sách của khách{productsInfo}{faqInfo}";
        }

        private string GetAdminSystemPrompt(RAGContext context)
        {
            return $@"Bạn là chuyên gia phân tích dữ liệu cho TechStore.
Bạn hỗ trợ quản trị viên:
1. Phân tích doanh thu, tồn kho
2. Xem hiệu suất bán hàng
3. Tạo báo cáo thống kê

Luôn trả lời ngắn gọn, đi thẳng vào vấn đề.
Nếu cần thực hiện action (thêm sản phẩm, sửa giá, v.v.), hãy mô tả action đó để admin tự thực hiện.";
        }
    }
}

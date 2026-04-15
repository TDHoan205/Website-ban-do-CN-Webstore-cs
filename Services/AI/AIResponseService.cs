using System.Text.Json;
using Webstore.Models.AI;

namespace Webstore.Services.AI
{
    /// <summary>
    /// AI Response Service - Tạo phản hồi AI cho chat
    /// </summary>
    public class AIResponseService
    {
        private readonly RAGEngineService _ragEngine;
        private readonly IntentDetectionService _intentDetector;

        public AIResponseService(RAGEngineService ragEngine, IntentDetectionService intentDetector)
        {
            _ragEngine = ragEngine;
            _intentDetector = intentDetector;
        }

        /// <summary>
        /// Tạo phản hồi AI cho tin nhắn
        /// </summary>
        public async Task<AIResponse> GenerateResponseAsync(string userMessage)
        {
            // 1. Get RAG context
            var context = await _ragEngine.GetContextAsync(userMessage);

            // 2. Detect intent
            var intent = _intentDetector.DetectIntent(userMessage);
            var confidence = _intentDetector.GetConfidenceScore(userMessage, intent);

            // 3. Generate response based on intent
            var response = GenerateBasedOnIntent(userMessage, intent, context);

            // 4. Check for escalation
            var shouldEscalate = _intentDetector.ShouldEscalate(intent, confidence);

            return new AIResponse
            {
                Message = response,
                Intent = intent,
                Confidence = confidence,
                ShouldEscalate = shouldEscalate,
                Products = context.Products,
                Faqs = context.FAQs
            };
        }

        /// <summary>
        /// Tạo response dựa trên intent
        /// </summary>
        private string GenerateBasedOnIntent(string userMessage, string intent, RAGContext context)
        {
            switch (intent)
            {
                case "greeting":
                    return GenerateGreetingResponse();

                case "thanks":
                    return "Cảm ơn bạn đã liên hệ! Nếu cần hỗ trợ gì thêm, mình sẵn sàng giúp bạn nhé. 😊";

                case "purchase":
                    return GeneratePurchaseResponse(context);

                case "warranty":
                    return GenerateWarrantyResponse(context);

                case "payment":
                    return GeneratePaymentResponse(context);

                case "shipping":
                    return GenerateShippingResponse(context);

                case "order_status":
                    return GenerateOrderStatusResponse(context);

                case "complaint":
                    return GenerateComplaintResponse();

                case "inquiry":
                    return GenerateInquiryResponse(context);

                default:
                    return GenerateGeneralResponse(context);
            }
        }

        private string GenerateGreetingResponse()
        {
            var greetings = new[]
            {
                "Xin chào! 👋 Mình là trợ lý TechStore. Mình có thể giúp bạn tìm sản phẩm công nghệ phù hợp, tư vấn về bảo hành, thanh toán hay giao hàng.",
                "Chào bạn! Rất vui được hỗ trợ bạn tại TechStore. Bạn đang quan tâm đến sản phẩm nào hôm nay?",
                "Hi! Mình là trợ lý AI của TechStore. Mình sẵn sàng giúp bạn tìm kiếm sản phẩm, tư vấn mua hàng, hay giải đáp thắc mắc về dịch vụ."
            };
            return greetings[DateTime.Now.Second % greetings.Length];
        }

        private string GeneratePurchaseResponse(RAGContext context)
        {
            var response = "Bạn đang tìm mua sản phẩm công nghệ phải không? ";

            if (context.Products.Count > 0)
            {
                response += "Mình gợi ý một số sản phẩm phù hợp:\n\n";

                foreach (var product in context.Products.Take(3))
                {
                    var formattedPrice = FormatPrice(product.Price);
                    response += $"• {product.Name}\n";
                    response += $"  💰 Giá: {formattedPrice}\n";
                    if (!string.IsNullOrEmpty(product.Specs))
                    {
                        var specs = ParseSpecs(product.Specs);
                        if (specs.Any())
                            response += $"  📋 {string.Join(", ", specs.Take(2))}\n";
                    }
                    response += $"\n";
                }

                response += "Bạn có muốn xem chi tiết sản phẩm nào không?";
            }
            else
            {
                response += "Bạn có thể cho mình biết thêm về nhu cầu sử dụng và ngân sách để mình tư vấn tốt hơn nhé!";
            }

            return response;
        }

        private string GenerateWarrantyResponse(RAGContext context)
        {
            var faq = context.FAQs.FirstOrDefault(f => f.Category == "warranty");
            if (faq != null)
            {
                return $"{faq.Answer}\n\nBạn có câu hỏi cụ thể nào về bảo hành không? Mình sẵn sàng hỗ trợ!";
            }

            return "TechStore hỗ trợ bảo hành chính hãng cho tất cả sản phẩm:\n\n" +
                   "• Điện thoại: Bảo hành 12-24 tháng tùy hãng\n" +
                   "• Laptop: Bảo hành 12-36 tháng\n" +
                   "• Phụ kiện: Bảo hành 6-12 tháng\n\n" +
                   "Để được hỗ trợ nhanh hơn, bạn có thể liên hệ hotline hoặc ghé cửa hàng gần nhất nhé!";
        }

        private string GeneratePaymentResponse(RAGContext context)
        {
            var faq = context.FAQs.FirstOrDefault(f => f.Category == "payment");
            if (faq != null)
            {
                return faq.Answer;
            }

            return "TechStore hỗ trợ nhiều phương thức thanh toán:\n\n" +
                   "• 💵 Thanh toán khi nhận hàng (COD)\n" +
                   "• 🏦 Chuyển khoản ngân hàng\n" +
                   "• 💳 Thẻ Visa/Mastercard\n" +
                   "• 📱 Ví điện tử: VNPay, MoMo, ZaloPay\n\n" +
                   "Thanh toán online được bảo mật qua cổng thanh toán VNPay. Bạn yên tâm nhé!";
        }

        private string GenerateShippingResponse(RAGContext context)
        {
            var faq = context.FAQs.FirstOrDefault(f => f.Category == "shipping");
            if (faq != null)
            {
                return faq.Answer;
            }

            return "Thông tin giao hàng tại TechStore:\n\n" +
                   "• 🚚 Nội thành: Giao trong 1-2 ngày\n" +
                   "• 🚛 Ngoại thành/Tỉnh: Giao trong 3-5 ngày\n" +
                   "• 📦 Phí ship: Miễn phí cho đơn từ 500.000đ\n" +
                   "• 💰 Dưới 500.000đ: Phí 30.000đ\n\n" +
                   "Bạn ở khu vực nào để mình tư vấn chính xác hơn nhé!";
        }

        private string GenerateOrderStatusResponse(RAGContext context)
        {
            return "Để kiểm tra trạng thái đơn hàng, bạn vui lòng:\n\n" +
                   "1️⃣ Đăng nhập vào tài khoản TechStore\n" +
                   "2️⃣ Vào mục \"Đơn hàng của tôi\"\n" +
                   "3️⃣ Chọn đơn hàng cần kiểm tra\n\n" +
                   "Tại đây bạn sẽ thấy chi tiết trạng thái: Đang xử lý → Đã xác nhận → Đang giao → Đã giao\n\n" +
                   "Nếu cần hỗ trợ thêm, bạn có thể liên hệ hotline: 1900.xxxx";
        }

        private string GenerateComplaintResponse()
        {
            return "Mình rất tiếc khi bạn không hài lòng. 😔\n\n" +
                   "TechStore luôn lắng nghe và hỗ trợ khách hàng. Bạn có thể:\n\n" +
                   "• Mô tả chi tiết vấn đề để mình hỗ trợ\n" +
                   "• Liên hệ hotline: 1900.xxxx\n" +
                   "• Hoặc bấm nút \"Gặp quản trị viên\" để được hỗ trợ trực tiếp\n\n" +
                   "Chúng tôi sẽ giải quyết nhanh chóng cho bạn!";
        }

        private string GenerateInquiryResponse(RAGContext context)
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
                   "• Giao hàng và vận chuyển\n" +
                   "• Tình trạng đơn hàng\n\n" +
                   "Bạn đang quan tâm đến vấn đề gì?";
        }

        private string GenerateGeneralResponse(RAGContext context)
        {
            if (context.Products.Count > 0)
            {
                var response = "Mình có một số gợi ý cho bạn:\n\n";

                foreach (var product in context.Products.Take(3))
                {
                    var formattedPrice = FormatPrice(product.Price);
                    response += $"📱 {product.Name}\n";
                    response += $"   Giá: {formattedPrice}\n\n";
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

        /// <summary>
        /// Format price to VND string
        /// </summary>
        private string FormatPrice(decimal price)
        {
            return price.ToString("N0") + "đ";
        }

        /// <summary>
        /// Parse specifications from JSON string
        /// </summary>
        private List<string> ParseSpecs(string specsJson)
        {
            try
            {
                if (string.IsNullOrEmpty(specsJson))
                    return new List<string>();

                var specs = JsonSerializer.Deserialize<Dictionary<string, string>>(specsJson);
                if (specs == null)
                    return new List<string>();

                var result = new List<string>();
                if (specs.TryGetValue("ram", out var ram) && !string.IsNullOrEmpty(ram))
                    result.Add($"RAM: {ram}");
                if (specs.TryGetValue("cpu", out var cpu) && !string.IsNullOrEmpty(cpu))
                    result.Add($"CPU: {cpu}");
                if (specs.TryGetValue("storage", out var storage) && !string.IsNullOrEmpty(storage))
                    result.Add($"Storage: {storage}");

                return result;
            }
            catch
            {
                return new List<string>();
            }
        }
    }

    /// <summary>
    /// AI Response - phản hồi từ AI
    /// </summary>
    public class AIResponse
    {
        public string Message { get; set; } = "";
        public string Intent { get; set; } = "";
        public decimal Confidence { get; set; }
        public bool ShouldEscalate { get; set; }
        public List<ProductContext> Products { get; set; } = new();
        public List<FAQ> Faqs { get; set; } = new();
    }
}

namespace Webstore.Services.AI
{
    /// <summary>
    /// Intent Detection - Nhận diện ý định của người dùng
    /// </summary>
    public class IntentDetectionService
    {
        private readonly Dictionary<string, List<string>> _intentKeywords = new()
        {
            ["purchase"] = new List<string> { "mua", "cần", "tìm", "muốn có", "đặt", "order", "mang", "lấy" },
            ["inquiry"] = new List<string> { "hỏi", "thông tin", "biết", "xem", "có gì", "cho hỏi", "muốn biết" },
            ["warranty"] = new List<string> { "bảo hành", "hỏng", "lỗi", "sửa", "đổi", "trả", "bh", "bao hanh" },
            ["payment"] = new List<string> { "thanh toán", "chuyển khoản", "cod", "vnpay", "tiền mặt", "visa", "thanh toan" },
            ["shipping"] = new List<string> { "giao", "ship", "vận chuyển", "nhận hàng", "giao hàng", "phí ship", "ship cod" },
            ["order_status"] = new List<string> { "đơn hàng", "theo dõi", "giao chưa", "đơn", "tình trạng đơn", "order", "đã đặt" },
            ["complaint"] = new List<string> { "phàn nàn", "không hài lòng", "khiếu nại", "tệ", "dở", "không tốt", "bực" },
            ["greeting"] = new List<string> { "xin chào", "chào", "hi", "hello", "hey", "alo", "chào buổi", "namaste" },
            ["thanks"] = new List<string> { "cảm ơn", "cam on", "thanks", "thank", "trân trọng" }
        };

        /// <summary>
        /// Detect user intent from message
        /// </summary>
        public string DetectIntent(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "unknown";

            var lowerMessage = message.ToLower().Trim();

            // Check for greeting first
            if (_intentKeywords["greeting"].Any(kw => lowerMessage.Contains(kw)))
                return "greeting";

            // Check for thanks
            if (_intentKeywords["thanks"].Any(kw => lowerMessage.Contains(kw)))
                return "thanks";

            // Check other intents
            foreach (var intent in _intentKeywords)
            {
                if (intent.Key == "greeting" || intent.Key == "thanks")
                    continue;

                if (intent.Value.Any(kw => lowerMessage.Contains(kw)))
                    return intent.Key;
            }

            return "general";
        }

        /// <summary>
        /// Get confidence score for detected intent
        /// </summary>
        public decimal GetConfidenceScore(string message, string intent)
        {
            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(intent))
                return 0.5m;

            var lowerMessage = message.ToLower();

            if (!_intentKeywords.ContainsKey(intent))
                return 0.5m;

            var keywords = _intentKeywords[intent];
            var matchCount = keywords.Count(kw => lowerMessage.Contains(kw));

            return Math.Min(0.5m + (matchCount * 0.15m), 1.0m);
        }

        /// <summary>
        /// Check if message should be escalated to admin
        /// </summary>
        public bool ShouldEscalate(string intent, decimal confidence)
        {
            // Escalate complaints and warranty issues
            if (intent == "complaint" || intent == "warranty")
                return true;

            // Escalate if low confidence
            if (confidence < 0.4m)
                return true;

            return false;
        }
    }
}

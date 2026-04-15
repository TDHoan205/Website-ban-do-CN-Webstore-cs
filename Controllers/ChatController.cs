using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Webstore.Data;
using Webstore.Models.AI;
using Webstore.Services.AI;

namespace Webstore.Controllers
{
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AIResponseService _aiResponseService;
        private readonly RAGEngineService _ragEngine;

        public ChatController(ApplicationDbContext context, AIResponseService aiResponseService, RAGEngineService ragEngine)
        {
            _context = context;
            _aiResponseService = aiResponseService;
            _ragEngine = ragEngine;
        }

        /// <summary>
        /// Trang Chat widget (partial view)
        /// </summary>
        public IActionResult Widget()
        {
            return View();
        }

        /// <summary>
        /// Trang Chat Admin Panel
        /// </summary>
        [Route("/admin/chat")]
        public async Task<IActionResult> AdminPanel()
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("Login", "Auth", new { returnUrl = "/admin/chat" });
            }

            // Get all active sessions
            var sessions = await _context.ChatSessions
                .Include(s => s.Account)
                .OrderByDescending(s => s.StartedAt)
                .ToListAsync();

            return View(sessions);
        }

        /// <summary>
        /// API: Gửi tin nhắn chat
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
            {
                return Json(new { success = false, error = "Tin nhắn không được để trống" });
            }

            try
            {
                var currentAccountId = GetCurrentAccountId();
                if (request.AccountId == null)
                {
                    request.AccountId = currentAccountId;
                }

                // Get or create session
                var sessionId = request.SessionId ?? Guid.NewGuid();
                var session = await GetOrCreateSession(sessionId, request.AccountId);

                // Save user message
                var userMessage = new ChatMessage
                {
                    SessionId = sessionId,
                    Message = request.Message,
                    SenderType = "user",
                    CreatedAt = DateTime.Now
                };
                _context.ChatMessages.Add(userMessage);

                // Generate AI response
                var aiResponse = await _aiResponseService.GenerateResponseAsync(request.Message);

                // Save AI response
                var aiMessage = new ChatMessage
                {
                    SessionId = sessionId,
                    Message = aiResponse.Message,
                    SenderType = "ai",
                    CreatedAt = DateTime.Now,
                    Metadata = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        intent = aiResponse.Intent,
                        confidence = aiResponse.Confidence,
                        shouldEscalate = aiResponse.ShouldEscalate
                    })
                };
                _context.ChatMessages.Add(aiMessage);

                // Log for AI evaluation
                var log = new AIConversationLog
                {
                    SessionId = sessionId,
                    UserMessage = request.Message,
                    AIResponse = aiResponse.Message,
                    IntentDetected = aiResponse.Intent,
                    ConfidenceScore = aiResponse.Confidence,
                    WasEscalated = aiResponse.ShouldEscalate,
                    CreatedAt = DateTime.Now
                };
                _context.AIConversationLogs.Add(log);

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    sessionId = sessionId,
                    userMessageId = userMessage.MessageId,
                    aiMessageId = aiMessage.MessageId,
                    message = aiResponse.Message,
                    intent = aiResponse.Intent,
                    confidence = aiResponse.Confidence,
                    shouldEscalate = aiResponse.ShouldEscalate,
                    products = aiResponse.Products.Select(p => new
                    {
                        id = p.ProductId,
                        name = p.Name,
                        price = p.Price,
                        category = p.Category,
                        specs = p.Specs,
                        imageUrl = p.ImageUrl
                    }),
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        /// <summary>
        /// API: Lấy lịch sử chat
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetHistory(Guid sessionId)
        {
            var messages = await _context.ChatMessages
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    messageId = m.MessageId,
                    senderType = m.SenderType,
                    message = m.Message,
                    createdAt = m.CreatedAt,
                    metadata = m.Metadata
                })
                .ToListAsync();

            return Json(new { success = true, messages });
        }

        /// <summary>
        /// API: Yêu cầu gặp Admin
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Escalate([FromBody] EscalateRequest request)
        {
            if (request.SessionId == Guid.Empty)
            {
                return Json(new { success = false, error = "Session không hợp lệ" });
            }

            var session = await _context.ChatSessions
                .Include(s => s.Account)
                .FirstOrDefaultAsync(s => s.SessionId == request.SessionId);
            if (session == null)
            {
                return Json(new { success = false, error = "Session không tìm thấy" });
            }

            // Update session status
            session.Status = "escalated";

            // Create notification for admins
            var admins = await _context.Accounts
                .Where(a => a.Role == "Admin" || a.Role == "admin")
                .ToListAsync();

            foreach (var admin in admins)
            {
                var notification = new Notification
                {
                    AccountId = admin.AccountId,
                    Type = "chat_escalated",
                    Message = $"Khách hàng {session.Account?.FullName ?? "Ẩn danh"} cần hỗ trợ qua chat",
                    Link = "/admin/chat",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(notification);
            }

            // Add system message
            var systemMessage = new ChatMessage
            {
                SessionId = request.SessionId,
                Message = "Yêu cầu của bạn đã được chuyển đến quản trị viên. Vui lòng chờ trong giây lát.",
                SenderType = "system",
                CreatedAt = DateTime.Now
            };
            _context.ChatMessages.Add(systemMessage);

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã chuyển yêu cầu đến quản trị viên" });
        }

        /// <summary>
        /// API: Admin nhận chat
        /// </summary>
        [HttpPost]
        [Route("/api/admin/chat/accept")]
        public async Task<IActionResult> AcceptChat([FromBody] AcceptChatRequest? request, Guid? sessionId = null)
        {
            if (!IsAdminUser())
            {
                return Json(new { success = false, error = "Bạn không có quyền truy cập" });
            }

            var targetSessionId = request?.SessionId ?? sessionId ?? Guid.Empty;
            if (targetSessionId == Guid.Empty)
            {
                return Json(new { success = false, error = "Session không hợp lệ" });
            }

            var session = await _context.ChatSessions.FindAsync(targetSessionId);
            if (session == null)
            {
                return Json(new { success = false, error = "Session không tìm thấy" });
            }

            var adminId = GetCurrentAccountId();
            if (!adminId.HasValue)
            {
                return Json(new { success = false, error = "Không xác định được admin" });
            }

            session.AssignedTo = adminId.Value;
            session.Status = "active";

            // Add welcome message from admin
            var welcomeMessage = new ChatMessage
            {
                SessionId = targetSessionId,
                Message = "Xin chào! Mình là quản trị viên TechStore. Mình có thể giúp gì cho bạn?",
                SenderType = "admin",
                CreatedAt = DateTime.Now
            };
            _context.ChatMessages.Add(welcomeMessage);

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        /// <summary>
        /// API: Admin gửi tin nhắn
        /// </summary>
        [HttpPost]
        [Route("/api/admin/chat/reply")]
        public async Task<IActionResult> AdminReply([FromBody] AdminReplyRequest request)
        {
            if (!IsAdminUser())
            {
                return Json(new { success = false, error = "Bạn không có quyền truy cập" });
            }

            if (string.IsNullOrWhiteSpace(request?.Message) || request.SessionId == Guid.Empty)
            {
                return Json(new { success = false, error = "Dữ liệu không hợp lệ" });
            }

            var adminId = GetCurrentAccountId();
            if (!adminId.HasValue)
            {
                return Json(new { success = false, error = "Không xác định được admin" });
            }

            var session = await _context.ChatSessions.FindAsync(request.SessionId);
            if (session == null)
            {
                return Json(new { success = false, error = "Session không tìm thấy" });
            }

            // Save admin message
            var adminMessage = new ChatMessage
            {
                SessionId = request.SessionId,
                Message = request.Message,
                SenderType = "admin",
                CreatedAt = DateTime.Now
            };
            _context.ChatMessages.Add(adminMessage);

            // Notify customer if logged in
            if (session.AccountId.HasValue)
            {
                var notification = new Notification
                {
                    AccountId = session.AccountId.Value,
                    Type = "chat_message",
                    Message = "Quản trị viên đã trả lời tin nhắn của bạn",
                    Link = "/chat",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        /// <summary>
        /// API: Kết thúc chat
        /// </summary>
        [HttpPost]
        [Route("/api/admin/chat/end")]
        public async Task<IActionResult> EndChat(Guid sessionId)
        {
            if (!IsAdminUser())
            {
                return Json(new { success = false, error = "Bạn không có quyền truy cập" });
            }

            var session = await _context.ChatSessions.FindAsync(sessionId);
            if (session == null)
            {
                return Json(new { success = false, error = "Session không tìm thấy" });
            }

            session.Status = "closed";
            session.EndedAt = DateTime.Now;

            // Add goodbye message
            var goodbyeMessage = new ChatMessage
            {
                SessionId = sessionId,
                Message = "Cảm ơn bạn đã chat với TechStore! Nếu cần hỗ trợ, hãy liên hệ lại nhé.",
                SenderType = "system",
                CreatedAt = DateTime.Now
            };
            _context.ChatMessages.Add(goodbyeMessage);

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        /// <summary>
        /// API: Lấy tin nhắn mới (polling)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNewMessages(Guid sessionId, int? lastMessageId)
        {
            var query = _context.ChatMessages
                .Where(m => m.SessionId == sessionId);

            if (lastMessageId.HasValue)
            {
                query = query.Where(m => m.MessageId > lastMessageId.Value);
            }

            var messages = await query
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    messageId = m.MessageId,
                    senderType = m.SenderType,
                    message = m.Message,
                    createdAt = m.CreatedAt
                })
                .ToListAsync();

            var session = await _context.ChatSessions.FindAsync(sessionId);
            return Json(new { success = true, messages, status = session?.Status ?? "active" });
        }

        /// <summary>
        /// API: Danh sách phiên chat cho Admin
        /// </summary>
        [HttpGet]
        [Route("/api/admin/chat/sessions")]
        public async Task<IActionResult> AdminSessions()
        {
            if (!IsAdminUser())
            {
                return Json(new { success = false, error = "Bạn không có quyền truy cập" });
            }

            var sessions = await _context.ChatSessions
                .Include(s => s.Account)
                .OrderByDescending(s => s.StartedAt)
                .Select(s => new
                {
                    sessionId = s.SessionId,
                    accountId = s.AccountId,
                    accountName = s.Account != null ? s.Account.FullName : "Khách vãng lai",
                    username = s.Account != null ? s.Account.Username : "guest",
                    status = s.Status,
                    assignedTo = s.AssignedTo,
                    startedAt = s.StartedAt,
                    endedAt = s.EndedAt,
                    lastMessageAt = _context.ChatMessages
                        .Where(m => m.SessionId == s.SessionId)
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => (DateTime?)m.CreatedAt)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Json(new { success = true, sessions });
        }

        /// <summary>
        /// API: Lấy lịch sử chat cho Admin theo session
        /// </summary>
        [HttpGet]
        [Route("/api/admin/chat/history")]
        public async Task<IActionResult> AdminHistory(Guid sessionId, int? lastMessageId)
        {
            if (!IsAdminUser())
            {
                return Json(new { success = false, error = "Bạn không có quyền truy cập" });
            }

            var query = _context.ChatMessages.Where(m => m.SessionId == sessionId);
            if (lastMessageId.HasValue)
            {
                query = query.Where(m => m.MessageId > lastMessageId.Value);
            }

            var messages = await query
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    messageId = m.MessageId,
                    senderType = m.SenderType,
                    message = m.Message,
                    createdAt = m.CreatedAt
                })
                .ToListAsync();

            return Json(new { success = true, messages });
        }

        /// <summary>
        /// Get or create chat session
        /// </summary>
        private async Task<ChatSession> GetOrCreateSession(Guid sessionId, int? accountId)
        {
            var session = await _context.ChatSessions.FindAsync(sessionId);
            if (session == null)
            {
                session = new ChatSession
                {
                    SessionId = sessionId,
                    AccountId = accountId,
                    Status = "active",
                    StartedAt = DateTime.Now
                };
                _context.ChatSessions.Add(session);
                await _context.SaveChangesAsync();
            }

            if (session.AccountId == null && accountId.HasValue)
            {
                session.AccountId = accountId;
                await _context.SaveChangesAsync();
            }

            return session;
        }

        private int? GetCurrentAccountId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(claimValue, out var accountId))
            {
                return accountId;
            }

            return HttpContext.Session.GetInt32("AccountId");
        }

        private bool IsAdminUser()
        {
            return User.IsInRole("Admin") ||
                   User.IsInRole("admin") ||
                   User.IsInRole("Employee") ||
                   User.IsInRole("employee");
        }
    }

    // Request/Response DTOs
    public class ChatMessageRequest
    {
        public Guid? SessionId { get; set; }
        public string Message { get; set; } = "";
        public int? AccountId { get; set; }
    }

    public class EscalateRequest
    {
        public Guid SessionId { get; set; }
        public string? Reason { get; set; }
    }

    public class AdminReplyRequest
    {
        public Guid SessionId { get; set; }
        public string Message { get; set; } = "";
    }

    public class AcceptChatRequest
    {
        public Guid SessionId { get; set; }
    }
}

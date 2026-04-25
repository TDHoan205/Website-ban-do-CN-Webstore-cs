using Webstore.Models.AI;

namespace Webstore.Data
{
    public static class AISeedData
    {
        public static List<FAQ> GetFAQs()
        {
            return new List<FAQ>
            {
                // General
                new FAQ
                {
                    Question = "TechStore có uy tín không?",
                    Answer = "TechStore là cửa hàng công nghệ uy tín với hơn 5 năm kinh nghiệm. Chúng tôi cam kết:\n• 100% sản phẩm chính hãng\n• Bảo hành theo tiêu chuẩn nhà sản xuất\n• Hỗ trợ khách hàng 24/7\n• Đổi trả trong 7 ngày nếu sản phẩm lỗi",
                    Category = "general",
                    Keywords = "uy tín, chính hãng, tin cậy, đáng tin",
                    Priority = 10,
                    IsActive = true
                },
                new FAQ
                {
                    Question = "Giờ làm việc của TechStore?",
                    Answer = "TechStore làm việc từ 8:00 - 21:00 các ngày trong tuần (Thứ 2 - Thứ 7).\n\nNgày nghỉ: Chủ nhật và ngày lễ.\n\nBạn có thể:\n• Đến cửa hàng trực tiếp\n• Gọi hotline: 1900.xxxx\n• Chat trực tuyến 24/7",
                    Category = "general",
                    Keywords = "giờ mở cửa, giờ làm việc, thời gian, mở cửa",
                    Priority = 8,
                    IsActive = true
                },

                // Purchase
                new FAQ
                {
                    Question = "Laptop có bảo hành không?",
                    Answer = "Có! Tất cả laptop tại TechStore đều được bảo hành chính hãng:\n\n• MacBook: Bảo hành 12 tháng AppleCare\n• Dell/Lenovo/ASUS: Bảo hành 24-36 tháng tùy model\n• Các hãng khác: 12-24 tháng\n\nBảo hành bao gồm: Lỗi phần cứng từ nhà sản xuất, đổi mới nếu lỗi nghiêm trọng trong 15 ngày đầu.",
                    Category = "purchase",
                    Keywords = "bảo hành, warranty, bh, bh laptop",
                    Priority = 10,
                    IsActive = true
                },
                new FAQ
                {
                    Question = "Có được trả góp không?",
                    Answer = "Có! TechStore hỗ trợ trả góp 0% lãi suất qua thẻ tín dụng:\n\n• VPBank, BIDV, Techcombank: Trả góp 6-12 tháng\n• HD Saison: Trả góp 6-24 tháng\n• HOME Credit: Trả góp 6-18 tháng\n\nĐiều kiện:\n• Tuổi: 21-60\n• Thẻ tín dụng hoặc CMND + GPLX\n• Thu nhập tối thiểu 5 triệu/tháng",
                    Category = "purchase",
                    Keywords = "trả góp, installment, lãi suất, trả chậm",
                    Priority = 9,
                    IsActive = true
                },
                new FAQ
                {
                    Question = "Laptop có nhiều màu không?",
                    Answer = "Tùy sản phẩm và hãng mà laptop có các tùy chọn màu sắc khác nhau:\n\n• MacBook Air/Pro: Space Gray, Silver, Gold, Midnight\n• Dell XPS: Bạc, Xám\n• ASUS ZenBook: Xám, Xanh đậm\n• HP Pavilion: Bạc, Vàng gold\n\nĐể biết chính xác màu sắc của sản phẩm, bạn nên xem chi tiết trên trang sản phẩm hoặc liên hệ cửa hàng.",
                    Category = "purchase",
                    Keywords = "màu sắc, màu, mau sac, color",
                    Priority = 5,
                    IsActive = true
                },

                // Payment
                new FAQ
                {
                    Question = "Chấp nhận thanh toán gì?",
                    Answer = "TechStore chấp nhận nhiều phương thức thanh toán:\n\n💵 Tiền mặt: Tại cửa hàng\n🏦 Chuyển khoản: Ngân hàng Vietcombank, Techcombank, VPBank\n💳 Thẻ: Visa, Mastercard, JCB\n📱 Ví điện tử: VNPay, MoMo, ZaloPay\n📦 COD: Thanh toán khi nhận hàng\n\nTất cả thanh toán online đều được bảo mật qua cổng VNPay.",
                    Category = "payment",
                    Keywords = "thanh toán, payment, chuyển khoản, ví điện tử, vnpay, momo",
                    Priority = 10,
                    IsActive = true
                },
                new FAQ
                {
                    Question = "Thanh toán online an toàn không?",
                    Answer = "Rất an toàn! TechStore sử dụng cổng thanh toán VNPay với:\n\n• Mã hóa SSL 256-bit\n• Xác thực 3D Secure (3DS)\n• Không lưu thông tin thẻ\n• Bảo mật theo chuẩn PCI DSS\n\nBạn hoàn toàn yên tâm khi thanh toán online tại TechStore.",
                    Category = "payment",
                    Keywords = "bảo mật, an toàn, secure, ssl, mã hóa",
                    Priority = 9,
                    IsActive = true
                },

                // Warranty
                new FAQ
                {
                    Question = "Bảo hành như thế nào?",
                    Answer = "TechStore hỗ trợ bảo hành theo tiêu chuẩn nhà sản xuất:\n\n📱 Điện thoại: 12-24 tháng tùy hãng\n💻 Laptop: 12-36 tháng tùy hãng\n🎧 Phụ kiện: 6-12 tháng\n\nCách thực hiện:\n1. Mang sản phẩm + hóa đơn đến cửa hàng\n2. Hoặc gửi mail bảo hành: warranty@techstore.vn\n3. Liên hệ hotline: 1900.xxxx",
                    Category = "warranty",
                    Keywords = "bảo hành, warranty, bh, sửa chữa, đổi trả",
                    Priority = 10,
                    IsActive = true
                },
                new FAQ
                {
                    Question = "Hết bảo hành thì sửa ở đâu?",
                    Answer = "TechStore có dịch vụ sửa chữa sau bảo hành:\n\n🔧 Thay màn hình, pin, sạc\n🔧 Sửa lỗi phần mềm\n🔧 Vệ sinh, bảo dưỡng\n\nGiá cả hợp lý, chất lượng đảm bảo. Liên hệ hotline hoặc ghé cửa hàng để được báo giá cụ thể.",
                    Category = "warranty",
                    Keywords = "sửa chữa, repair, hết bảo hành, sau bh",
                    Priority = 7,
                    IsActive = true
                },
                new FAQ
                {
                    Question = "Chính sách đổi trả như thế nào?",
                    Answer = "TechStore hỗ trợ đổi trả theo quy định:\n\n✅ Đổi mới: Trong 7 ngày nếu sản phẩm lỗi từ nhà sản xuất\n✅ Đổi size/màu: Trong 3 ngày với sản phẩm có nhiều tùy chọn\n✅ Hoàn tiền: Trong 5 ngày làm việc sau khi nhận lại sản phẩm\n\nĐiều kiện: Sản phẩm còn nguyên vẹn, đầy đủ phụ kiện, hộp và hóa đơn.",
                    Category = "warranty",
                    Keywords = "đổi trả, return, hoàn tiền, refund",
                    Priority = 9,
                    IsActive = true
                },

                // Shipping
                new FAQ
                {
                    Question = "Giao hàng trong bao lâu?",
                    Answer = "TechStore giao hàng nhanh chóng:\n\n🚚 Nội thành TP.HCM/HN: 1-2 ngày\n🚛 Ngoại thành/Tỉnh: 3-5 ngày\n📦 Hỏa tốc: Trong 4 giờ (phụ phí 50.000đ)\n\nĐơn hàng trước 17h sẽ được giao trong ngày (nội thành).",
                    Category = "shipping",
                    Keywords = "giao hàng, ship, vận chuyển, delivery, thời gian giao",
                    Priority = 10,
                    IsActive = true
                },
                new FAQ
                {
                    Question = "Phí ship bao nhiêu?",
                    Answer = "Phí giao hàng tại TechStore:\n\n📦 Miễn phí: Đơn hàng từ 500.000đ\n💰 30.000đ: Đơn dưới 500.000đ (nội thành)\n💰 50.000đ: Đơn dưới 500.000đ (ngoại thành)\n🚚 80.000đ: Tỉnh khác\n\nKhách hàng VIP và thành viên TechStore Gold được miễn phí vận chuyển mọi đơn hàng.",
                    Category = "shipping",
                    Keywords = "phí ship, phí vận chuyển, shipping fee, free ship",
                    Priority = 9,
                    IsActive = true
                },

                // Order Status
                new FAQ
                {
                    Question = "Tôi muốn hủy đơn?",
                    Answer = "Bạn có thể hủy đơn hàng theo các cách:\n\n1️⃣ Tự hủy: Vào \"Đơn hàng của tôi\" → Chọn đơn → \"Hủy đơn\"\n2️⃣ Hotline: Gọi 1900.xxxx trong giờ làm việc\n3️⃣ Chat: Nhắn tin qua chatbot hoặc fanpage\n\nLưu ý:\n• Chỉ hủy được trong 2 giờ đầu sau khi đặt\n• Đơn đã giao cho đơn vị vận chuyển không thể hủy",
                    Category = "general",
                    Keywords = "hủy đơn, cancel order, hủy, cancel",
                    Priority = 8,
                    IsActive = true
                },
                new FAQ
                {
                    Question = "Làm sao theo dõi đơn hàng?",
                    Answer = "Theo dõi đơn hàng dễ dàng:\n\n📱 App TechStore: \"Đơn hàng của tôi\" → Chọn đơn → Xem trạng thái\n🌐 Website: Đăng nhập → Tài khoản → Đơn hàng\n📧 Email: Theo dõi mã vận đơn trong email xác nhận\n📞 Hotline: Gọi 1900.xxxx với mã đơn hàng\n\nTrạng thái đơn: Đang xử lý → Đã xác nhận → Đang giao → Đã giao",
                    Category = "general",
                    Keywords = "theo dõi, track, đơn hàng, order status, tình trạng",
                    Priority = 9,
                    IsActive = true
                }
            };
        }
    }
}

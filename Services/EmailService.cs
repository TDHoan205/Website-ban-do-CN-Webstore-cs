using System.Net;
using System.Net.Mail;
using System.Text;

namespace Webstore.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            if (!emailSettings.Exists())
            {
                Console.WriteLine("EmailSettings is not configured in appsettings.json");
                return false;
            }

            var smtpHost = emailSettings["SmtpHost"];
            var smtpPort = emailSettings["SmtpPort"];
            var enableSsl = emailSettings["EnableSsl"];
            var fromEmail = emailSettings["FromEmail"];
            var fromName = emailSettings["FromName"];
            var username = emailSettings["Username"];
            var password = emailSettings["Password"];

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Console.WriteLine("Email configuration is incomplete");
                return false;
            }

            try
            {
                using var client = new SmtpClient(smtpHost, int.TryParse(smtpPort, out var port) ? port : 587);
                client.EnableSsl = bool.TryParse(enableSsl, out var ssl) && ssl;
                client.Credentials = new NetworkCredential(username, password);
                client.Timeout = 10000;

                var mail = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName ?? "Webstore"),
                    Subject = "Đặt lại mật khẩu - Webstore",
                    Body = BuildPasswordResetEmailBody(resetLink),
                    IsBodyHtml = true,
                    SubjectEncoding = Encoding.UTF8,
                    BodyEncoding = Encoding.UTF8
                };
                mail.To.Add(toEmail);

                await client.SendMailAsync(mail);
                Console.WriteLine($"Password reset email sent to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
                return false;
            }
        }

        private string BuildPasswordResetEmailBody(string resetLink)
        {
            return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #f4f4f4;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f4f4f4; padding: 30px 15px;'>
        <tr>
            <td align='center'>
                <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'>
                    <!-- Header -->
                    <tr>
                        <td style='background: linear-gradient(135deg, #023E8A, #0077B6); padding: 30px; text-align: center;'>
                            <h1 style='color: #ffffff; margin: 0; font-size: 24px; font-weight: bold;'>Webstore</h1>
                            <p style='color: #CAF0F8; margin: 8px 0 0 0; font-size: 14px;'>Cửa hàng trực tuyến hàng đầu</p>
                        </td>
                    </tr>
                    <!-- Content -->
                    <tr>
                        <td style='padding: 40px 30px;'>
                            <h2 style='color: #023E8A; margin: 0 0 20px 0; font-size: 20px;'>Yêu cầu đặt lại mật khẩu</h2>
                            <p style='color: #333333; font-size: 15px; line-height: 1.6; margin: 0 0 20px 0;'>
                                Chúng tôi đã nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.
                            </p>
                            <p style='color: #333333; font-size: 15px; line-height: 1.6; margin: 0 0 30px 0;'>
                                Nhấp vào nút bên dưới để đặt lại mật khẩu của bạn. Liên kết này sẽ hết hạn sau <strong>30 phút</strong>.
                            </p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{resetLink}' style='display: inline-block; background: linear-gradient(135deg, #0077B6, #00B4D8); color: #ffffff; text-decoration: none; padding: 14px 40px; border-radius: 6px; font-weight: bold; font-size: 16px;'>Đặt lại mật khẩu</a>
                            </div>
                            <p style='color: #666666; font-size: 13px; line-height: 1.6; margin: 0 0 15px 0;'>
                                Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này hoặc liên hệ với chúng tôi nếu bạn có thắc mắc.
                            </p>
                            <hr style='border: none; border-top: 1px solid #E1E8ED; margin: 25px 0;'>
                            <p style='color: #999999; font-size: 12px; line-height: 1.6; margin: 0;'>
                                Liên kết đặt lại mật khẩu: <a href='{resetLink}' style='color: #0077B6;'>{resetLink}</a>
                            </p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style='background-color: #f8f9fa; padding: 20px 30px; text-align: center; border-top: 1px solid #E1E8ED;'>
                            <p style='color: #666666; font-size: 12px; margin: 0;'>
                                © 2024 Webstore. Tất cả quyền được bảo lưu.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }
    }
}

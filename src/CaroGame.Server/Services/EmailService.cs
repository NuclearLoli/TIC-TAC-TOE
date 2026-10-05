using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CaroGame.Server.Services;

public interface IEmailService
{
    Task<(bool Success, string Message)> SendOtpEmailAsync(string toEmail, string otpCode, string purpose);
}

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> SendOtpEmailAsync(string toEmail, string otpCode, string purpose)
    {
        string host = _config["Smtp:Host"] ?? "smtp.gmail.com";
        int port = int.TryParse(_config["Smtp:Port"], out int p) ? p : 587;
        string senderEmail = _config["Smtp:SenderEmail"] ?? "carogame.nu@gmail.com";
        string senderPassword = _config["Smtp:SenderPassword"] ?? string.Empty;
        string displayName = _config["Smtp:SenderDisplayName"] ?? "Cờ Caro Arena";

        if (string.IsNullOrWhiteSpace(senderPassword))
        {
            _logger.LogWarning("SMTP SenderPassword is not configured. OTP code: {OtpCode} for {ToEmail}", otpCode, toEmail);
            return (false, "Chưa cấu hình mật khẩu gửi email trên máy chủ.");
        }

        string subject;
        string title;
        string description;

        if (string.Equals(purpose, "ResetPassword", StringComparison.OrdinalIgnoreCase))
        {
            subject = "[Caro Arena] Mã OTP khôi phục mật khẩu";
            title = "Khôi Phục Mật Khẩu Tài Khoản";
            description = "Bạn (hoặc ai đó) vừa yêu cầu đặt lại mật khẩu cho tài khoản Caro Arena. Hãy nhập mã OTP 6 số dưới đây:";
        }
        else
        {
            subject = "[Caro Arena] Mã OTP xác thực đăng ký tài khoản";
            title = "Xác Thực Địa Chỉ Email";
            description = "Cảm ơn bạn đã tham gia Cờ Caro Arena! Để hoàn tất đăng ký tài khoản, vui lòng nhập mã xác thực OTP 6 số dưới đây:";
        }

        string htmlBody = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""utf-8"">
  <title>{subject}</title>
</head>
<body style=""margin:0; padding:0; background-color:#1E1D1B; font-family:-apple-system, Segoe UI, Roboto, Helvetica, Arial, sans-serif; color:#E2DFD8;"">
  <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""background-color:#1E1D1B; padding:30px 10px;"">
    <tr>
      <td align=""center"">
        <table width=""520"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""background-color:#262421; border:1px solid #3F3C38; border-radius:16px; overflow:hidden; box-shadow:0 10px 25px rgba(0,0,0,0.5);"">
          <!-- Header -->
          <tr>
            <td align=""center"" style=""background-color:#181715; padding:24px; border-bottom:1px solid #36332E;"">
              <span style=""font-size:22px; font-weight:900; color:#EF4444; margin-right:4px;"">✕</span>
              <span style=""font-size:22px; font-weight:900; color:#38BDF8; margin-right:8px;"">◯</span>
              <span style=""font-size:20px; font-weight:900; letter-spacing:1px; color:#FFFFFF;"">CỜ CARO ARENA</span>
            </td>
          </tr>
          <!-- Body -->
          <tr>
            <td style=""padding:32px 28px;"">
              <h2 style=""margin:0 0 14px 0; font-size:18px; color:#FFFFFF; text-align:center;"">{title}</h2>
              <p style=""margin:0 0 20px 0; font-size:14px; line-height:1.6; color:#B0ADA9; text-align:center;"">{description}</p>
              
              <!-- OTP Box -->
              <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""margin:24px 0;"">
                <tr>
                  <td align=""center"">
                    <div style=""display:inline-block; background-color:#1E1D1B; border:2px dashed #81B64C; border-radius:12px; padding:16px 36px; text-align:center;"">
                      <span style=""font-size:34px; font-weight:900; letter-spacing:8px; color:#81B64C; font-family:Consolas, 'Courier New', monospace;"">{otpCode}</span>
                    </div>
                  </td>
                </tr>
              </table>

              <p style=""margin:0 0 8px 0; font-size:12.5px; color:#EAB308; text-align:center; font-weight:bold;"">⏳ Mã xác thực có hiệu lực trong vòng 5 phút.</p>
              <p style=""margin:0; font-size:12px; color:#75736F; text-align:center;"">Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email. Tuyệt đối không chia sẻ mã này cho bất kỳ ai.</p>
            </td>
          </tr>
          <!-- Footer -->
          <tr>
            <td align=""center"" style=""background-color:#181715; padding:16px; border-top:1px solid #36332E; font-size:11px; color:#6B6966;"">
              © {DateTime.UtcNow.Year} Caro Arena Online • Chess.com Style Multiplayer Engine
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

        try
        {
            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true,
                Timeout = 15000
            };

            using var message = new MailMessage
            {
                From = new MailAddress(senderEmail, displayName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
            _logger.LogInformation("Successfully sent OTP email to {ToEmail} for {Purpose}", toEmail, purpose);
            return (true, "Mã xác thực đã được gửi đến email của bạn.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP email to {ToEmail}", toEmail);
            return (false, $"Lỗi gửi email: {ex.Message}");
        }
    }
}

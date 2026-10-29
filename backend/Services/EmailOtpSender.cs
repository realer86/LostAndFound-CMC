using System.Net;
using System.Net.Mail;

namespace LostFound.Api.Services;

public sealed class EmailOtpSender(IConfiguration config, IWebHostEnvironment environment)
{
    public bool IsDevelopmentCodeEnabled => environment.IsDevelopment() && config.GetValue<bool>("Auth:AllowDevCode");

    public async Task<bool> SendAsync(string to, string code, CancellationToken cancellationToken)
    {
        // Opt-in testing only. No real email is sent in this mode unless SMTP is configured.
        var host = config["Auth:Smtp:Host"];
        var username = config["Auth:Smtp:Username"];
        var password = config["Auth:Smtp:Password"];
        var from = config["Auth:Smtp:From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(from))
        {
            if (IsDevelopmentCodeEnabled) return false;
            throw new InvalidOperationException("SMTP is not configured.");
        }

        using var client = new SmtpClient(host, config.GetValue("Auth:Smtp:Port", 587))
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 15000
        };
        using var mail = new MailMessage(new MailAddress(from, "Foundly"), new MailAddress(to))
        {
            Subject = "Mã xác minh tài khoản Foundly",
            Body = $"Mã xác minh của bạn là: {code}\nMã có hiệu lực trong 10 phút. Không chia sẻ mã này với bất kỳ ai.",
            IsBodyHtml = false
        };
        await client.SendMailAsync(mail, cancellationToken);
        return true;
    }
}

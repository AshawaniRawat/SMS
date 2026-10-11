using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using SchoolERP.Application.Features.Authentication;

namespace SchoolERP.Infrastructure.Identity;

public sealed class SmtpEmailService(IOptions<EmailDeliverySettings> options) : IEmailService
{
    public async Task SendPasswordResetEmailAsync(string email, string resetLink,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Host) || string.IsNullOrWhiteSpace(settings.From))
            throw new InvalidOperationException("Email delivery is not configured.");

        using var message = new MailMessage(settings.From, email)
        {
            Subject = "Reset your SchoolERP password",
            Body = $"A password reset was requested for your SchoolERP account.\n\n" +
                   $"Open this link to choose a new password:\n{resetLink}\n\n" +
                   "This link expires soon. If you did not request this reset, you can ignore this email.",
            IsBodyHtml = false
        };
        using var smtp = new SmtpClient(settings.Host, settings.Port) { EnableSsl = settings.EnableSsl };
        if (!string.IsNullOrWhiteSpace(settings.Username))
            smtp.Credentials = new NetworkCredential(settings.Username, settings.Password);
        await smtp.SendMailAsync(message, cancellationToken);
    }
}

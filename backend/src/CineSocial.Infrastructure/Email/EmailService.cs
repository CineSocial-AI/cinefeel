using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace CineSocial.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            var smtpHost = _configuration["SMTP_HOST"] ?? throw new InvalidOperationException("SMTP_HOST is not configured");
            var smtpPort = int.Parse(_configuration["SMTP_PORT"] ?? "587");
            var smtpUsername = _configuration["SMTP_USERNAME"] ?? throw new InvalidOperationException("SMTP_USERNAME is not configured");
            var smtpPassword = _configuration["SMTP_PASSWORD"] ?? throw new InvalidOperationException("SMTP_PASSWORD is not configured");
            var smtpFromEmail = _configuration["SMTP_FROM_EMAIL"] ?? smtpUsername;
            var smtpFromName = _configuration["SMTP_FROM_NAME"] ?? "CineSocial";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(smtpFromName, smtpFromEmail));
            message.To.Add(new MailboxAddress(to, to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = body
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Gmail için SSL/TLS yapılandırması
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls, cancellationToken);
            await client.AuthenticateAsync(smtpUsername, smtpPassword, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            throw new InvalidOperationException($"Email sending failed: {ex.Message}", ex);
        }
    }

    public async Task SendEmailVerificationAsync(string email, string username, string token, CancellationToken cancellationToken = default)
    {
        var verificationBaseUrl = _configuration["EMAIL_VERIFICATION_URL"] ?? "http://localhost:5500/verify.html";
        var verificationUrl = $"{verificationBaseUrl}?token={token}";

        // Read HTML template
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Email", "Templates", "EmailVerification.html");

        string htmlBody;
        if (File.Exists(templatePath))
        {
            htmlBody = await File.ReadAllTextAsync(templatePath, cancellationToken);
        }
        else
        {
            // Fallback basit HTML
            _logger.LogWarning("Email template not found at {TemplatePath}, using fallback", templatePath);
            htmlBody = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
</head>
<body style='font-family: Arial, sans-serif; padding: 20px;'>
    <div style='max-width: 600px; margin: 0 auto;'>
        <h2>Merhaba {{USERNAME}},</h2>
        <p>CineSocial'e hoş geldin! Email adresini doğrulamak için aşağıdaki linke tıkla:</p>
        <p><a href='{{VERIFICATION_URL}}' style='background-color: #667eea; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>Email'imi Doğrula</a></p>
        <p>Bu link 24 saat boyunca geçerlidir.</p>
        <p>Saygılarımızla,<br>CineSocial Ekibi</p>
    </div>
</body>
</html>";
        }

        // Replace placeholders
        htmlBody = htmlBody.Replace("{{USERNAME}}", username);
        htmlBody = htmlBody.Replace("{{VERIFICATION_URL}}", verificationUrl);

        await SendEmailAsync(email, "CineSocial - Email Doğrulama", htmlBody, cancellationToken);
    }
}

using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace AlbansLodgingHouse.Services;

public record EmailAttachment(string FileName, byte[] Content, string ContentType);

public class EmailService
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _user;
    private readonly string _password;
    private readonly string _displayName;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _host = configuration["Smtp:Host"] ?? "smtp.gmail.com";
        _port = int.TryParse(configuration["Smtp:Port"], out var port) ? port : 587;
        _user = configuration["Smtp:User"] ?? "";
        _password = configuration["Smtp:Password"] ?? "";
        _displayName = configuration["Smtp:DisplayName"] ?? "Alban's Lodging House";
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_user) && !string.IsNullOrWhiteSpace(_password);

    public async Task SendAsync(
        string toEmail,
        string? toName,
        string subject,
        string htmlBody,
        IEnumerable<EmailAttachment>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("SMTP credentials are not configured; skipping email to {ToEmail} ({Subject}).", toEmail, subject);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_displayName, _user));
        message.To.Add(new MailboxAddress(toName ?? toEmail, toEmail));
        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = htmlBody };
        if (attachments is not null)
        {
            foreach (var attachment in attachments)
            {
                builder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
            }
        }
        message.Body = builder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_host, _port, SecureSocketOptions.StartTls, cancellationToken);
            await client.AuthenticateAsync(_user, _password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail} ({Subject}).", toEmail, subject);
        }
    }
}

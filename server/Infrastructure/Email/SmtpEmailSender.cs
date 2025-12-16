using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using OblivionDrive.Application.Shared;

namespace OblivionDrive.Infrastructure.Orm.Email;
public class SmtpEmailSender(IOptions<EmailSettings> options) : IEmailSender
{
    private readonly EmailSettings settings = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ValidateSettings(settings);

        var mimeMessage = new MimeMessage();

        mimeMessage.From.Add(new MailboxAddress(settings.FromName, settings.FromEmail));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));
        mimeMessage.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder { TextBody = message.Body };

        foreach (var attachment in message.Attachments)
        {
            bodyBuilder.Attachments.Add(
                attachment.FileName,
                attachment.Content,
                ContentType.Parse(attachment.ContentType));
        }

        mimeMessage.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        var secureOption = settings.UseStartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.Auto;

        await client.ConnectAsync(settings.Host, settings.Port, secureOption, cancellationToken);
        await client.AuthenticateAsync(settings.Username, settings.Password, cancellationToken);
        await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private static void ValidateSettings(EmailSettings emailSettings)
    {
        if (string.IsNullOrWhiteSpace(emailSettings.Host))
            throw new InvalidOperationException("Email:Host não configurado.");

        if (emailSettings.Port <= 0)
            throw new InvalidOperationException("Email:Port inválido.");

        if (string.IsNullOrWhiteSpace(emailSettings.Username))
            throw new InvalidOperationException("Email:Username não configurado.");

        if (string.IsNullOrWhiteSpace(emailSettings.Password))
            throw new InvalidOperationException("Email:Password não configurado.");

        if (string.IsNullOrWhiteSpace(emailSettings.FromEmail))
            throw new InvalidOperationException("Email:FromEmail não configurado.");

        if (string.IsNullOrWhiteSpace(emailSettings.FromName))
            throw new InvalidOperationException("Email:FromName não configurado.");
    }
}

public class EmailSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool UseStartTls { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
}
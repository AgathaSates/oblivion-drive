namespace OblivionDrive.Application.Shared;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

public record EmailMessage(
    string To,
    string Subject,
    string Body,
    IReadOnlyCollection<EmailAttachment> Attachments
);

public record EmailAttachment(
    string FileName,
    string ContentType,
    byte[] Content
);
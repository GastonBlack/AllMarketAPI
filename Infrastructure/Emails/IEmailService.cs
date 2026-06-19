namespace AllMarket.Infrastructure.Emails;

public interface IEmailService
{
    Task SendAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlContent,
        CancellationToken cancellationToken = default);
}

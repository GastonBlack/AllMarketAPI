using AllMarket.Infrastructure.Emails;

namespace AllMarketAPI.Tests;

public sealed class TestEmailService : IEmailService
{
    public Task SendAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlContent,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

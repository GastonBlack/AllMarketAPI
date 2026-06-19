using System.Net.Http.Json;

namespace AllMarket.Infrastructure.Emails;

public class BrevoEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BrevoEmailService> _logger;

    public BrevoEmailService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<BrevoEmailService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    private string GetRequiredConfiguration(string primaryKey, string fallbackKey)
    {
        var value = _configuration[primaryKey] ?? _configuration[fallbackKey];

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{primaryKey} is not configured.");

        return value;
    }

    public async Task SendAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlContent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            throw new ArgumentException("Recipient email is required.", nameof(toEmail));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Email subject is required.", nameof(subject));

        if (string.IsNullOrWhiteSpace(htmlContent))
            throw new ArgumentException("Email content is required.", nameof(htmlContent));

        var apiKey = GetRequiredConfiguration("Brevo:ApiKey", "BREVO_API_KEY");
        var senderEmail = GetRequiredConfiguration("Brevo:SenderEmail", "BREVO_SENDER_EMAIL");
        var senderName = _configuration["Brevo:SenderName"]
            ?? _configuration["BREVO_SENDER_NAME"]
            ?? "AllMarket";

        using var request = new HttpRequestMessage(HttpMethod.Post, "smtp/email");
        request.Headers.Add("api-key", apiKey);
        request.Content = JsonContent.Create(new
        {
            sender = new
            {
                email = senderEmail,
                name = senderName
            },
            to = new[]
            {
                new
                {
                    email = toEmail,
                    name = toName
                }
            },
            subject,
            htmlContent
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
            return;

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogError(
            "Brevo email send failed with status {StatusCode}: {Response}",
            response.StatusCode,
            error);

        throw new InvalidOperationException("Brevo could not send the email.");
    }
}

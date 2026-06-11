using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AllMarket.Infrastructure.Exceptions;

namespace AllMarket.Infrastructure.Images;

public class CloudinaryImageStorageService : IImageStorageService
{
    private const long MaxImageSize = 5 * 1024 * 1024;

    // Dependencies
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public CloudinaryImageStorageService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    // Validation and Cloudinary credentials
    private (string CloudName, string ApiKey, string ApiSecret) GetCredentials()
    {
        var cloudinaryUrl = _configuration["CLOUDINARY_URL"];

        if (string.IsNullOrWhiteSpace(cloudinaryUrl))
            throw new InvalidOperationException("Cloudinary is not configured.");

        var uri = new Uri(cloudinaryUrl);
        var credentials = uri.UserInfo.Split(':', 2);

        if (uri.Scheme != "cloudinary" || credentials.Length != 2)
            throw new InvalidOperationException("Cloudinary configuration is invalid.");

        return (
            uri.Host,
            Uri.UnescapeDataString(credentials[0]),
            Uri.UnescapeDataString(credentials[1]));
    }

    private static void ValidateImage(IFormFile image)
    {
        if (image.Length == 0)
            throw new BadRequestException("The selected image is empty.");

        if (image.Length > MaxImageSize)
            throw new BadRequestException("The image must be 5 MB or smaller.");

        if (!image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException("The selected file must be an image.");
    }

    // Upload
    public async Task<string> UploadProductImageAsync(IFormFile image)
    {
        ValidateImage(image);

        var (cloudName, apiKey, apiSecret) = GetCredentials();
        const string folder = "allmarket/products";

        using var content = new MultipartFormDataContent();
        await using var imageStream = image.OpenReadStream();
        using var imageContent = new StreamContent(imageStream);

        imageContent.Headers.ContentType =
            MediaTypeHeaderValue.Parse(image.ContentType);
        content.Add(new StringContent(folder), "folder");
        content.Add(imageContent, "file", Path.GetFileName(image.FileName));

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://api.cloudinary.com/v1_1/{cloudName}/image/upload")
        {
            Content = content
        };
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        using var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            await using var errorStream = await response.Content.ReadAsStreamAsync();
            var errorResponse = await JsonSerializer.DeserializeAsync<CloudinaryErrorResponse>(
                errorStream);
            var errorMessage = errorResponse?.Error?.Message;

            throw new BadRequestException(string.IsNullOrWhiteSpace(errorMessage)
                ? "The product image could not be uploaded."
                : $"The product image could not be uploaded: {errorMessage}");
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        var upload = await JsonSerializer.DeserializeAsync<CloudinaryUploadResponse>(
            responseStream);

        return upload?.SecureUrl
            ?? throw new BadRequestException("Cloudinary did not return an image URL.");
    }

    private sealed class CloudinaryUploadResponse
    {
        [JsonPropertyName("secure_url")]
        public string? SecureUrl { get; set; }
    }

    private sealed class CloudinaryErrorResponse
    {
        [JsonPropertyName("error")]
        public CloudinaryError? Error { get; set; }
    }

    private sealed class CloudinaryError
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}

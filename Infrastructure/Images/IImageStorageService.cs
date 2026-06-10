namespace AllMarket.Infrastructure.Images;

public interface IImageStorageService
{
    Task<string> UploadProductImageAsync(IFormFile image);
}

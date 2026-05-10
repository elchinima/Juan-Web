using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;

namespace Juan_NET.Web.Services
{
    public class ImageStorageService
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png",
            ".jpeg",
            ".jpg",
            ".gif"
        };

        public async Task<string> SaveAsWebpAsync(IFormFile file, string folder)
        {
            var extension = Path.GetExtension(file.FileName);

            if (!AllowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Only png, jpeg, jpg and gif images are allowed.");
            }

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folder);
            Directory.CreateDirectory(uploadsPath);

            var fileName = $"{Guid.NewGuid():N}.webp";
            var filePath = Path.Combine(uploadsPath, fileName);

            await using var input = file.OpenReadStream();
            using var image = await Image.LoadAsync(input);
            await image.SaveAsWebpAsync(filePath, new WebpEncoder
            {
                Quality = 40
            });

            return $"/uploads/{folder}/{fileName}";
        }

        public void DeleteUpload(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.GetFullPath(Path.Combine(wwwrootPath, relativePath));
            var uploadsPath = Path.GetFullPath(Path.Combine(wwwrootPath, "uploads"));

            if (!filePath.StartsWith(uploadsPath, StringComparison.OrdinalIgnoreCase) || !File.Exists(filePath))
            {
                return;
            }

            File.Delete(filePath);
        }
    }
}

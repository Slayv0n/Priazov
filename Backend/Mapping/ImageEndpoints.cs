using Backend.Models.Dto;
using DataBase.Models;

namespace Backend.Mapping
{
    public static class ImageEndpoints
    {
        public static void MapImageEnpoints(this WebApplication app)
        {
            var group = app.MapGroup("/images");
            group.MapPost("/create", Create).Accepts<ImageUploadDto>("multipart/form-data")
                .Produces<ImageResponseDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .WithName("UploadUserImage")
                .WithTags("Images");
            group.MapGet("user/{userId}/{imageId}", Image);
        }

        private static async Task<IResult> Create(HttpContext context, IWebHostEnvironment env)
        {
            var form = await context.Request.ReadFormAsync();
            var file = form.Files["file"];

            if (file == null || file.Length == 0)
                return Results.BadRequest("Файл не предоставлен");

            if (!Guid.TryParse(form["userId"], out var userId))
                return Results.BadRequest("User ID не предоставлен");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                return Results.BadRequest("Недопустимый формат файла");

            if (file.Length > 10 * 1024 * 1024)
                return Results.BadRequest("Файл слишком большой (макс. 10MB)");

            var imageId = Guid.NewGuid();
            var fileName = $"{imageId}{extension}";
            var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);

            var userImagesPath = Path.Combine(env.WebRootPath, "uploads", "users", userId.ToString());

            if (!Directory.Exists(userImagesPath))
                Directory.CreateDirectory(userImagesPath);

            var userFilePath = Path.Combine(userImagesPath, fileName);
            using (var stream = new FileStream(userFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var request = context.Request;
            var imageUrl = $"{request.Scheme}://{request.Host}/uploads/users/{userId}/{fileName}";

            var response = new ImageResponseDto
            {
                Id = imageId,
                FileName = fileName,
                OriginalName = originalFileName,
                Url = imageUrl,
                Size = file.Length,
                UserId = userId,
                UploadDate = DateTime.UtcNow
            };

            return Results.Ok(response);
        }
        private static async Task<IResult> Image(
            Guid userId,
            Guid imageId,
            HttpContext context,
            IWebHostEnvironment env)
        {
            var userImagesPath = Path.Combine(env.WebRootPath, "uploads", "users", userId.ToString());

            if (!Directory.Exists(userImagesPath))
                return Results.NotFound("Папка пользователя не найдена");

            // Ищем файл по ID (имя файла = imageId + расширение)
            var files = Directory.GetFiles(userImagesPath, $"{imageId}.*");

            if (files.Length == 0)
                return Results.NotFound("Изображение не найдено");

            var filePath = files[0];
            var fileInfo = new FileInfo(filePath);
            var fileName = Path.GetFileName(filePath);
            var extension = Path.GetExtension(filePath);
            var originalName = Path.GetFileNameWithoutExtension(filePath);

            var imageResponse = new ImageResponseDto
            {
                Id = imageId,
                FileName = fileName,
                OriginalName = originalName,
                Url = $"{context.Request.Scheme}://{context.Request.Host}/uploads/users/{userId}/{fileName}",
                Size = fileInfo.Length,
                UserId = userId,
                UploadDate = fileInfo.CreationTimeUtc
            };

            return Results.Ok(imageResponse);
        }
    }
}

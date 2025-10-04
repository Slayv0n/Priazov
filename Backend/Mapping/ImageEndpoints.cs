using Backend.Models.Dto;
using Backend.Services;
using DataBase.Models;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Mapping
{
    public static class ImageEndpoints
    {
        public static void MapImageEnpoints(this WebApplication app)
        {
            var group = app.MapGroup("/images").WithTags("Images");
            group.MapPost("/upload", Upload).Accepts<ImageUploadDto>("multipart/form-data")
                .Produces<ImageResponseDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest);
            group.MapGet("/avatar/{userId}", AvatarImage)
                .Produces<ImageResponseDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest);
            group.MapGet("/main/{userId}", MainImage)
                .Produces<ImageResponseDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest);
        }

        private static async Task<IResult> Upload(
            [FromForm] IFormFile file,
            [FromForm] Guid userId,
            [FromForm] bool isAvatar,
            HttpContext context,
            [FromServices] IImageService service)
        {
            if (file == null || file.Length == 0)
                return Results.BadRequest("Файл не предоставлен");

            var image = await service.Upload(file, userId, isAvatar);

            if (image == null)
            {
                return Results.BadRequest("Не удалось загрузить изображение");
            }

            var request = context.Request;
            var url = $"{request.Scheme}://{request.Host}/uploads/users/{userId}/{image.FileName}";

            var response = new ImageResponseUrlDto
            {
                Id = image.Id,
                FileName = image.FileName,
                OriginalName = image.OriginalName,
                Size = image.Size,
                Url = url,
                UserId = userId,
                IsAvatar = isAvatar
            };

            return Results.Ok(response);
        }
        private static async Task<IResult> AvatarImage(
            Guid userId,
            [FromServices] IImageService service)
        {
            var image = await service.Image(userId, true);

            var response = new ImageResponseUrlDto
            {
                Id = image.Id,
                FileName = image.FileName,
                OriginalName = image.OriginalName,
                Size = image.Size,
                Url = $"/uploads/users/{userId}/{image.FileName}",
                UserId = userId,
                IsAvatar = true
            };

            return Results.Ok(response);
        }
        private static async Task<IResult> MainImage(
           Guid userId,
           [FromServices] IImageService service)
        {
            var image = await service.Image(userId, false);

            if (image == null)
                return Results.NotFound("Изображение не найдено");

            var response = new ImageResponseUrlDto
            {
                Id = image.Id,
                FileName = image.FileName,
                OriginalName = image.OriginalName,
                Size = image.Size,
                Url = $"/uploads/users/{userId}/{image.FileName}",
                UserId = userId,
                IsAvatar = false
            };

            return Results.Ok(response);
        }
    }
}

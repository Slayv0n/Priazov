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
            HttpContext context,
            [FromServices] IImageService service)
        {
            var form = await context.Request.ReadFormAsync();
            var file = form.Files["file"];

            if (file == null || file.Length == 0)
                return Results.BadRequest("Файл не предоставлен");

            if (!Guid.TryParse(form["userId"], out var userId))
                return Results.BadRequest("User ID не предоставлен");

            if (!bool.TryParse(form["isAvatar"], out var isAvatar))
                return Results.BadRequest("Не предоставлена информация о типе изображения");

            var image = await service.Upload(file, userId, isAvatar);

            if (image == null)
            {
                return Results.BadRequest();
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
            HttpContext context,
            [FromServices] IImageService service)
        {
            var image = await service.Image(userId, true);

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
                IsAvatar = true
            };

            return Results.Ok(response);
        }
        private static async Task<IResult> MainImage(
           HttpContext context,
           Guid userId,
           [FromServices] IImageService service)
        {
            var image = await service.Image(userId, false);

            var response = image as ImageResponseUrlDto;

            if (response == null)
            {
                return Results.BadRequest();
            }

            var request = context.Request;
            var url = $"{request.Scheme}://{request.Host}/uploads/users/{userId}/{image.FileName}";

            response.Url = url;

            return Results.Ok(response);
        }
    }
}

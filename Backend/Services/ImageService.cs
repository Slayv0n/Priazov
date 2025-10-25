using Backend.Models.Dto;
using Backend.Validation;
using Dadata.Model;
using DataBase;
using DataBase.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.IO;
using static System.Net.Mime.MediaTypeNames;

namespace Backend.Services
{
    public interface IImageService
    {
        Task<ImageResponseDto> Upload(IFormFile? file, Guid userId, bool isAvatar);
        Task<ImageResponseDto> Update(IFormFile? file, Guid userId);
        Task<ImageResponseDto> Delete(IFormFile? file, Guid userId);
        Task<ImageResponseDto> Image(Guid userId, bool isAvatar);
    }

    public class ImageService : IImageService
    {
        private ImageResponseDto _default = new ImageResponseDto
        {
            Id = default,
            FileName = "",
            OriginalName = "",
            Size = default,
            UserId = default,
            UploadDate = default,
        };
        private IWebHostEnvironment _env;
        private IDbContextFactory<PriazovContext> _factory;
        private ILogger<ImageService> _logger;
        public ImageService(
            IWebHostEnvironment env,
            IDbContextFactory<PriazovContext> factory,
            ILogger<ImageService> logger)
        {
            _env = env;
            _factory = factory;
            _logger = logger;
        }

        public async Task<ImageResponseDto> Image(Guid userId, bool isAvatar)
        {
            var db = await _factory.CreateDbContextAsync();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogError($"Пользователь не найден {userId}");
                throw new NotFoundException("Пользователь не найден");
            }

            var userImagesPath = Path.Combine(_env.WebRootPath, "uploads", "users", userId.ToString());

            if (!Directory.Exists(userImagesPath))
            {
                _logger.LogError($"Папка пользователя не найдена {userId}");
                return _default;
            }

            Guid imageId = default;

            if (isAvatar)
            {
                imageId = user.AvatarId;
            }
            else
            {
                var company = user as Company;
                if (company != null)
                    imageId = company.MainId;
            }

            var files = Directory.GetFiles(userImagesPath, $"{imageId}.*");

            if (files.Length == 0)
            {
                _logger.LogError($"Аватар пользователя не найден {userId}");
                return _default;
            }

            var filePath = files[0];
            var fileInfo = new FileInfo(filePath);
            var fileName = Path.GetFileName(filePath);
            var extension = Path.GetExtension(filePath);
            var originalName = Path.GetFileNameWithoutExtension(filePath);

            var image = new ImageResponseDto
            {
                Id = imageId,
                FileName = fileName,
                OriginalName = originalName,
                Size = fileInfo.Length,
                UserId = userId,
                UploadDate = fileInfo.CreationTimeUtc,
                IsAvatar = isAvatar
            };

            return image;
        }

        public async Task<ImageResponseDto> Upload(IFormFile? file, Guid userId, bool isAvatar)
        {
            using var db = await _factory.CreateDbContextAsync();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogError($"Пользователь не найден {userId}");
                throw new NotFoundException("Пользователь не найден");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
            {
                _logger.LogError($"Недопустимый формат файла {extension}");
                throw new NotImplementedException("Недопустимый формат файла");
            }
            if (file.Length > 10 * 1024 * 1024)
            {
                _logger.LogError($"Файл слишком большой {file.Length}");
                throw new NotImplementedException("Файл слишком большой (макс. 10MB)");
            }

            var imageId = Guid.NewGuid();
            var fileName = $"{imageId}{extension}";
            var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);

            var userImagesPath = Path.Combine(_env.WebRootPath, "uploads", "users", userId.ToString());

            if (!Directory.Exists(userImagesPath))
                Directory.CreateDirectory(userImagesPath);

            var userFilePath = Path.Combine(userImagesPath, fileName);
            using (var stream = new FileStream(userFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var response = new ImageResponseDto
            {
                Id = imageId,
                FileName = fileName,
                OriginalName = originalFileName,
                Size = file.Length,
                UserId = userId,
                IsAvatar = isAvatar
            };

            if (isAvatar)
            {
                user.AvatarId = imageId;
            }
            else
            {
                var company = user as Company;
                if (company != null)
                    company.MainId = imageId;
            }

            await db.SaveChangesAsync();

            return response;

        }

        public Task<ImageResponseDto> Delete(IFormFile? file, Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task<ImageResponseDto> Update(IFormFile? file, Guid userId)
        {
            throw new NotImplementedException();
        }
    }
}

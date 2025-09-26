using Microsoft.AspNetCore.Http.HttpResults;

namespace Backend.Models.Dto
{
    public class ImageUploadDto
    {
        public required IFormFile File { get; set; }
        public Guid UserId { get; set; }
        public bool IsAvatar { get; set; }
    }

    public class ImageResponseDto
    {
        public Guid Id { get; set; }
        public required string FileName { get; set; }
        public required string OriginalName { get; set; }
        public required long Size { get; set; }
        public Guid UserId { get; set; }
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;
        public bool IsAvatar { get; set; }
    }

    public class ImageResponseUrlDto : ImageResponseDto
    {
        public required string Url { get; set; }

    }

}

using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Dto
{
    public record LoginDto(
    [Required] string Email,
    [Required] string Password
    );
    public record RefreshDto(
        [Required] string RefreshToken
    );
    public class AuthDto
    {
        public Guid Id { get; set; }
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }
    }
}

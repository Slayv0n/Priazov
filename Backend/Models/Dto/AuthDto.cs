using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Dto
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Введите электронную почту.")]
        [EmailAddress(ErrorMessage = "Неверный формат почты.")]
        [Display(Name = "Электронная почта")]
        public string Email { get; set; } = null!;
        [Required(ErrorMessage = "Введите пароль.")]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = null!;
    }
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

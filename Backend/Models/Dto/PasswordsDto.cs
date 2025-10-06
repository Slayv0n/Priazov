using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Dto
{
    public class PasswordChangeDto
    {
        public Guid UserId { get; set; }
        [Required(ErrorMessage = "Введите пароль.")]
        public required string Password { get; set; }
        [Required(ErrorMessage = "Введите новый пароль.")]
        [StringLength(30, MinimumLength = 8,
        ErrorMessage = "Длина пароля 8-30 символов.")]
        [RegularExpression(@"^\s*(?=.*[a-zа-яё])(?=.*[A-ZА-ЯЁ])(?=.*\d)(?=.*[^\da-zA-Zа-яА-ЯЁё]).{8,30}\s*$",
        ErrorMessage = "Пароль должен содержать заглавную и строчную буквы, цифру и спецсимвол.")]
        [Display(Name = "Пароль")]
        public required string NewPassword { get; set; }
        [Required(ErrorMessage = "Повторите новый пароль.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Пароли не совпадают.")]
        [Display(Name = "Повторите пароль")]
        public required string NewPassword2 { get; set; }
    }
}

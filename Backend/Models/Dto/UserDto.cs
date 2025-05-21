using Backend.Validation;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Dto
{
    public abstract class UserDto
    {
        [Required]
        [StringLength(100, MinimumLength = 8,
            ErrorMessage = "Длина названия/имени 8-100 символов")]
        [RegularExpression(@"^\s*[\p{L}\s]+\s*$",
            ErrorMessage = "Разрешены только буквы и пробелы")]
        public string Name { get; set; } = null!;
        [Required]
        [StringLength(254, MinimumLength = 5,
            ErrorMessage = "Длина почты 5-254 символов")]
        [RegularExpression(@"^\s*[\p{L}0-9_%+-]+(?<!\.)@(?!\.)[\p{L}.-]+\.[\p{L}]{2,4}\s*$",
            ErrorMessage = "Неверный формат почты")]
        public string Email { get; set; } = null!;
        [Required]
        [StringLength(18, MinimumLength = 10,
            ErrorMessage = "Длина номера телефона 10-18 символов")]
        [RegularExpression(
        @"^\s*(\+7|7|8)?[\s-]?\(?[0-9]{3}\)?[\s-]?[0-9]{3}[\s-]?[0-9]{2}[\s-]?[0-9]{2}\s*$",
        ErrorMessage = "Неверный формат телефона. Используйте российский номер")]
        public string Phone { get; set; } = null!;
        [Required]
        [StringLength(255, MinimumLength = 10, ErrorMessage = "Адрес должен содержать 10-255 символов")]
        //[AddressValidation(ErrorMessage = "Неверный формат адреса")]
        public string FullAddress { get; set; } = null!;
    }
}

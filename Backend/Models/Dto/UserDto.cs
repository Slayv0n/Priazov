using Backend.Validation;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Dto
{
    public abstract class UserDto
    {
        [Required(ErrorMessage="Введите название организации.")]
        [StringLength(100, MinimumLength = 8,
            ErrorMessage = "Длина названия/имени 8-100 символов.")]
        [RegularExpression(@"^\s*[\p{L}\d\s""'№().,-]+\s*$",
            ErrorMessage = "Разрешены только буквы, пробелы и специальные знаки.")]
        [Display(Name = "Название организации")]
        public string Name { get; set; } = null!;
        [Required(ErrorMessage="Введите электронную почту.")]
        [StringLength(254, MinimumLength = 5,
            ErrorMessage = "Длина почты 5-254 символов.")]
        [EmailAddress(ErrorMessage = "Неверный формат почты.")]
        [Display(Name="Электронная почта")]
        public string Email { get; set; } = null!;
        [Required(ErrorMessage = "Введите номер телефона.")]
        [StringLength(18, MinimumLength = 10,
            ErrorMessage = "Длина номера телефона 10-18 символов.")]
        [Phone(ErrorMessage = "Неверный формат телефона.")]
        [Display(Name = "Номер телефона")]
        public string Phone { get; set; } = null!;
        [Required(ErrorMessage = "Введите юридический адрес.")]
        [StringLength(255, MinimumLength = 10, ErrorMessage = "Адрес должен содержать 10-255 символов.")]
        [Display(Name="Юридический адрес")]
        //[AddressValidation(ErrorMessage = "Неверный формат адреса")]
        public string FullAddress { get; set; } = null!;
    }
}

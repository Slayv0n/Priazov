using DataBase.Models;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Dto
{
    public class ManagerCreateDto : UserDto
    {
        [Required]
        [StringLength(30, MinimumLength = 8,
            ErrorMessage = "Длина пароля 8-30 символов")]
        //[RegularExpression(@"^\s*(?=.*[a-zа-яё])(?=.*[A-ZА-ЯЁ])(?=.*\d)(?=.*[^\da-zA-Zа-яА-ЯЁё]).{8,30}\s*$",
        //ErrorMessage = "Пароль слишком слабый")]
        public string Password { get; set; } = null!;
        [Required]
        public string Token { get; set; } = null!;
    }

    public class ManagerResponseDto : UserDto
    {
        public Guid Id { get; set; }
        public byte[]? PhotoIcon { get; set; }

        public ManagerResponseDto() { }
        public ManagerResponseDto(Manager manager)
        {
            Id = manager.Id;
            Name = manager.Name;
            Email = manager.Email;
            Phone = manager.Phone;
            FullAddress = manager.Address.FullAddress;
        }
    }
    public class ManagerChangeDto : UserDto
    {
        public byte[]? PhotoIcon { get; set; }
    }
}
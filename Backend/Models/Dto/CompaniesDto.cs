using Backend.Validation;
using DataBase.Models;
using JsonProperty.EFCore;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Dto
{
    public class CompanyDto : UserDto
    {
        [Required(ErrorMessage = "Введите сферу интересов.")]
        [IndustryValidation]
        [Display(Name="Сфера интересов")]
        public string Industry { get; set; } = null!;
        [Required(ErrorMessage = "Введите ФИО руководителя.")]
        [StringLength(100, MinimumLength = 4,
            ErrorMessage = "Длина ФИО 4-100 символов.")]
        [RegularExpression(@"^\s*[\p{L}\s]+\s*$",
            ErrorMessage = "Разрешены только буквы и пробелы.")]
        [Display(Name= "ФИО руководителя")]
        public string LeaderName { get; set; } = null!;

    }
    public class CompanyCreateDto : CompanyDto
    {
        [Required(ErrorMessage="Введите пароль.")]
        [StringLength(30, MinimumLength = 8,
            ErrorMessage = "Длина пароля 8-30 символов.")]
        [RegularExpression(@"^\s*(?=.*[a-zа-яё])(?=.*[A-ZА-ЯЁ])(?=.*\d)(?=.*[^\da-zA-Zа-яА-ЯЁё]).{8,30}\s*$",
        ErrorMessage = "Пароль должен содержать заглавную и строчную буквы, цифру и спецсимвол.")]
        [Display(Name="Пароль")]
        public string Password { get; set; } = null!;
        [Required(ErrorMessage = "Повторите пароль.")]
        [Compare(nameof(Password), ErrorMessage = "Пароли не совпадают.")]
        [Display(Name = "Повторите пароль")]
        public string Password2 { get; set; } = null!;
        //[Required]
        //public string Token { get; set; } = null!;
    }
    public class CompanyResponseDto : CompanyDto
    {
        public Guid Id { get; set; }
        [MaxLength(1024)]
        public string? Description { get; set; }
        public Guid AvatarId { get; set; }
        public Guid MainId { get; set; }
        public JsonList<string> Contacts { get; set; } = new JsonList<string>();
        public CompanyResponseDto() { }
        public CompanyResponseDto(Company company, string address)
        {
            Id = company.Id;
            Name = company.Name;
            Email = company.Email;
            Phone = company.Phone;
            FullAddress = address;
            Industry = company.Industry;
            LeaderName = company.LeaderName;
            AvatarId = company.AvatarId;
            MainId = company.MainId;
            Contacts = company.Contacts;
            Description = company.Description;
        }
    }

    public class CompanyChangeDto : CompanyDto
    {
        [JsonListValidation]
        public JsonList<string> Contacts { get; set; } = new JsonList<string>();
        public string? Description { get; set; }
        public Guid AvatarId { get; set; }
        public Guid MainId { get; set; }
    }
}

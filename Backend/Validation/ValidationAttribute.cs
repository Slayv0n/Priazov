using JsonProperty.EFCore;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Backend.Validation
{
    public class JsonListValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext context)
        {       
            var Jsonlist = value as JsonList<string>;
            var regex = new Regex(@"^(https?:\/\/)?(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)$");
            if (Jsonlist == null)
                return new ValidationResult("Объект не является json объектом");

            var list = Jsonlist.VirtualList;
            
            if (list == null || list.Count == 0)
                return ValidationResult.Success!;

            list = list.Select(x => x.Trim()).ToList();

            if (list.Any(c => !regex.IsMatch(c)))
                return new ValidationResult("Контакты не проходят валидацию");
            return ValidationResult.Success!;
        }
    }

    public class IndustryValidationAttribute : ValidationAttribute
    {
        private readonly HashSet<string> _allowedIndustries = new()
        {
            "Образовательное учреждение",
            "Научно-исследовательский институт",
            "Научно-образовательный проект",
            "Государственное учреждение",
            "Коммерческая деятельность",
            "Стартап",
            "Финансы",
            "Акселератор / инкубатор / технопарк",
            "Ассоциация / объединение",
            "Государственное учреждение",
            "Некоммерческая организация",
            "Другое"
        };
        protected override ValidationResult IsValid(object? value, ValidationContext context)
        {       
            var industry = value as string;
            if (industry == null)
            {
                return new ValidationResult("Объект не является строкой");
            }
                
            if (!_allowedIndustries.Contains(industry))
            {
                return new ValidationResult("Недопустимое значение сферы интересов");
            }

            return ValidationResult.Success!;
        }
    }
}
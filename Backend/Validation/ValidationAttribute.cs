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
}
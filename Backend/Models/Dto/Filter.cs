using Backend.Validation;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Dto
{
    public class Filter
    {
        [Required(ErrorMessage = "Введите сферу интересов.")]
        [IndustryValidation(ErrorMessage = "Недопустимое значение сферы интересов.")]
        [Display(Name = "Сфера интересов")]
        public string Industry { get; set; } = null!;
        public bool IsChecked { get; set; }
    }
}

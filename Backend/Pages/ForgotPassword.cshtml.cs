using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Backend.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly IMessageService _messageService;

        [BindProperty]
        [Required(ErrorMessage="¬ведите электронную почту.")]
        [EmailAddress]
        [Display(Name="Ёлектронна€ почта")]
        public string Email { get; set; } ="";

        public ForgotPasswordModel(IMessageService messageService)
        {
            _messageService = messageService;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostEmailAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                
            }
        }
    }
}

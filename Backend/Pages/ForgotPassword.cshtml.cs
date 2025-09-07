using Backend.Services;
using Dadata.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Backend.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly IPasswordService _passwordService;
        public string? ValidationResult { get; set; } = "";
        public bool IsTokenSend = false;

        public ForgotPasswordModel(IPasswordService passwordService)
        {
            _passwordService = passwordService;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostEmailAsync([FromBody] EmailInput email)
        {
            TryValidateModel(email, nameof(email));

            if (!ModelState.IsValid)
            {
                return new JsonResult(new
                {
                    success = false,
                    errors = ModelState.ToDictionary(
                    k => k.Key,
                    v => v.Value.Errors.Select(e => e.ErrorMessage).ToArray())
                });
            }
            try
            {
                await _passwordService.ForgotPassword(email.Email);
                IsTokenSend = true;
            }
            catch(Exception ex)
            {
                ValidationResult = ex.Message;
                var Error = new
                {
                    success = false,
                    errors = new Dictionary<string, string[]>()
                };
                Error.errors.Add("Email", new string[] { ValidationResult });
                return new JsonResult(Error);
            }

            return new JsonResult(new
            {
                success = true
            });
        }

        public async Task<IActionResult> OnPostTokenAsync([FromBody] TokenInput token)
        {
            TryValidateModel(token, nameof(token));

            if (!ModelState.IsValid)
            {
                ValidationResult = ModelState.ToDictionary().FirstOrDefault(x => x.Key == "Token").Value?.ToString();
                return Page();
            }    
            try
            {
                await _passwordService.IsValidToken(token.Token);
            }
            catch (Exception ex)
            {
                ValidationResult = ex.Message;
                return Page();
            }

            return RedirectToPage("Index");
        }

        public class EmailInput 
        {
            [Required(ErrorMessage = "¬ведите электронную почту.")]
            [EmailAddress]
            [Display(Name = "Ёлектронна€ почта")]
            public string Email { get; set; } = null!;
        }
        public class TokenInput
        {
            [Required(ErrorMessage = "¬ведите код.")]
            [StringLength(6, MinimumLength = 6, ErrorMessage = "Ќедопустимый формат кода")]
            [Display(Name = "¬ведите код")]
            public string Token { get; set; } = null!;
        }
    }
}

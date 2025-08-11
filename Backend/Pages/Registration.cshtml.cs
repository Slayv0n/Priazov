using Backend.Models.Dto;
using Backend.Services;
using Backend.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Runtime.CompilerServices;

namespace Backend.Pages
{
    public class RegistrationModel : PageModel
    {
        private readonly ICompanyService _companyService;
        [BindProperty]
        public CompanyCreateDto Company { get; set; } = null!;
        public string ValidationResult { get; set; } = "";
        public RegistrationModel(ICompanyService companyService)
        {
            _companyService = companyService;
        }
        public void OnGet()
        {
        }
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            try
            {
                await _companyService.CreateCompanyAsync(Company);
            }
            catch (Exception ex)
            {
                ValidationResult = ex.Message;
                return Page();
            }
            
            return RedirectToPage("Index");
        }
    }
}

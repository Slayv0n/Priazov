using Backend.Models.Dto;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Backend.Pages
{
    public class RegistrationModel : PageModel
    {
        private readonly CompanyService _companyService;
        [BindProperty]
        public CompanyCreateDto Company { get; set; } = null!;
        [BindProperty]
        string CompanyName { get; set; } = "";
        public RegistrationModel(CompanyService companyService)
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
            await _companyService.CreateCompanyAsync(Company);
            return RedirectToPage("Index");
        }
    }
}

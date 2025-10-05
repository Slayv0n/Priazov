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
        private readonly IConfiguration _configuration;
        [BindProperty]
        public CompanyCreateDto Company { get; set; } = null!;
        public string ValidationResult { get; set; } = "";
        public RegistrationModel(ICompanyService companyService, IConfiguration configuration)
        {
            _companyService = companyService;
            _configuration = configuration;
        }
        public void OnGet()
        {
            ViewData["ApiKey"] = _configuration["Dadata:ApiKey"];
        }
    }
}

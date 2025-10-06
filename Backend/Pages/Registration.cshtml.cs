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

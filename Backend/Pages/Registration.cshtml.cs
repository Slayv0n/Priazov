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
        private readonly IConfiguration _configuration;
        public RegistrationModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void OnGet()
        {
            ViewData["ApiKey"] = _configuration["Dadata:ApiKey"];
        }
    }
}

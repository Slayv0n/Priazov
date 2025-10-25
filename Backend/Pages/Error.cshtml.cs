using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Backend.Pages
{
    public class ErrorModel : PageModel
    {
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }

        public void OnGet(string errorCode, string errorMessage)
        {
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }
    }
}

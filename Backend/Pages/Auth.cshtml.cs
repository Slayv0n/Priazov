using Backend.Models.Dto;
using Backend.Services;
using DataBase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Backend.Pages
{
    public class AuthModel : PageModel
    {
        private IAuthService _authService;
        public string ValidationResult { get; set; } = "";
        public AuthModel(IAuthService authService)
        {
            _authService = authService;
        }

        public void OnGet()
        {
        }
        public async Task<IActionResult> OnPostAsync(LoginDto login)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            try
            {
                await _authService.Login(login);
            }
            catch (Exception ex)
            {
                ValidationResult = ex.Message;
            }

            return RedirectToPage("Index");
        }
        public async Task<IActionResult> OnPostRefreshAsync(RefreshDto refresh)
        {            
            if (!ModelState.IsValid)
            {
                return new JsonResult(null);
            }

            AuthDto user;

            try
            {
                user = await _authService.Refresh(refresh);
            }
            catch (Exception ex)
            {
                return new JsonResult(ex.Message);
            }

            return new JsonResult(user);
        }

        public async Task<IActionResult> OnPostLogoutASync(RefreshDto refresh)
        {
            if (!ModelState.IsValid)
            {
                return new JsonResult(null);
            }
            try
            {
                await _authService.Logout(refresh);
            }
            catch (Exception ex)
            {
                return new JsonResult(ex.Message);
            }
            return new JsonResult(null);
        }
    }
}

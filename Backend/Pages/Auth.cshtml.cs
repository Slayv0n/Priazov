using Backend.Models;
using Backend.Models.Dto;
using Backend.Services;
using DataBase.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Backend.Pages
{
    public class AuthModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly JwtSettings _jwtSettings;
        public string ValidationResult { get; set; } = "";
        [BindProperty]
        public LoginDto Login { get; set; } = null!;
        public AuthModel(IAuthService authService, IOptions<JwtSettings> jwtSettings)
        {
            _authService = authService;
            _jwtSettings = jwtSettings.Value;
        }

        public void OnGet()
        {
        }
        public async Task<IActionResult> OnPostAsync()
        {
            AuthDto? User;
            ClaimsPrincipal? ClaimsPrincipal;
            if (!ModelState.IsValid)
            {
                return Page();
            }
            try
            {
                User = await _authService.Login(Login);
                ClaimsPrincipal = await _authService.SignInForContext(Login);
            }
            catch (Exception ex)
            {
                ValidationResult = ex.Message;
                return Page();
            }

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                ClaimsPrincipal,
                new AuthenticationProperties
                {
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
                    IsPersistent = true,
                    AllowRefresh = true
                });

            Response.Cookies.Append("refresh_token", User.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)
            });
            Response.Cookies.Append("access_token", User.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes)
            });

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

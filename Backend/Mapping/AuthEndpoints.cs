using Backend.Models;
using Backend.Models.Dto;
using Backend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Backend.Mapping
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/auth");

            group.MapPost("/login", Login);
            group.MapPost("/logout", Logout);
        }

        private static async Task<IResult> Login(HttpContext context,
            LoginDto loginDto,
            IAuthService service,
            ILogger<AuthService> logger,
            IOptions<JwtSettings> settings)
        {
            AuthDto? User;
            ClaimsPrincipal? ClaimsPrincipal;
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(
                loginDto,
                new ValidationContext(loginDto),
                validationResults,
                validateAllProperties: true
            );

            if (!isValid)
            {
                logger.LogWarning($"Ошибка валидации при создании инвестора: {validationResults}");
                var errors = validationResults
                    .GroupBy(v => v.MemberNames.FirstOrDefault() ?? "")
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(v => v.ErrorMessage ?? "Неизвестная ошибка").ToArray()
                    );
                return Results.ValidationProblem(errors);
            }

            User = await service.Login(loginDto);
            ClaimsPrincipal = await service.SignInForContext(loginDto);

            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                ClaimsPrincipal,
                new AuthenticationProperties
                {
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(settings.Value.AccessTokenExpiryMinutes),
                    IsPersistent = true,
                    AllowRefresh = true
                });

            context.Response.Cookies.Append("refresh_token", User.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(settings.Value.RefreshTokenExpiryDays)
            });
            context.Response.Cookies.Append("access_token", User.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(settings.Value.AccessTokenExpiryMinutes)
            });

            return Results.Ok(User);
        }
        private static async Task<IResult> Logout(HttpContext context,
            RefreshDto refreshDto,
            IAuthService service,
            ILogger<AuthService> logger,
            IOptions<JwtSettings> settings)
        {
            await service.Logout(refreshDto);
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.Response.Cookies.Delete("refresh_token");
            context.Response.Cookies.Delete("access_token");
            return Results.Ok();
        }
    }
}


using Backend.Models.Dto;
using Backend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.ComponentModel.DataAnnotations;

namespace Backend.Mapping
{
    public static class PasswordEndpoints
    {
        public static void MapPasswordEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/password");

            group.MapPost("/change", Change);
        }

        private static async Task<IResult> Change(PasswordChangeDto passwordChangeDto,
            HttpContext context,
            IPasswordService passwordService,
            IAuthService authService,
            ILogger<PasswordService> logger)
        {
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(
                passwordChangeDto,
                new ValidationContext(passwordChangeDto),
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

            if (!(await passwordService.IsValidPassword(passwordChangeDto.UserId, passwordChangeDto.Password)))
            {
                return Results.BadRequest();
            }

            var refreshDto = new RefreshDto { RefreshToken = context.Request.Cookies["refresh_token"] ?? "" };
            await authService.Logout(refreshDto);
            await passwordService.ResetPassword(passwordChangeDto.UserId, passwordChangeDto.NewPassword);          
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.Response.Cookies.Delete("refresh_token");
            context.Response.Cookies.Delete("access_token");

            return Results.Ok();
        }
    }
}

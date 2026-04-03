using Backend.Models.Dto;
using Backend.Services;
using System.ComponentModel.DataAnnotations;

namespace Backend.Mapping
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/auth").WithTags("Auth");
            group.MapPost("/login", Login)
                .Produces<AuthDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest);
            group.MapPost("/refresh", Refresh)
                .Produces<AuthDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest);
            group.MapPost("/logout", Logout)
                .Produces<AuthDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest);
        }

        private static async Task<IResult> Login(
            LoginDto loginDto,
            IAuthService service,
            ILogger<IAuthService> logger)
        {
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(
                loginDto,
                new ValidationContext(loginDto),
                validationResults,
                validateAllProperties: true
            );

            if (!isValid)
            {
                logger.LogWarning($"Ошибка валидации при попытке входа: {validationResults}");
                var errors = validationResults
                    .GroupBy(v => v.MemberNames.FirstOrDefault() ?? "")
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(v => v.ErrorMessage ?? "Неизвестная ошибка").ToArray()
                    );
                return Results.ValidationProblem(errors);
            }

            var response = await service.Login(loginDto);

            return Results.Ok(response);
        }

        private static async Task<IResult> Logout(
            RefreshDto refreshDto,
            IAuthService service,
            ILogger<IAuthService> logger)
        {
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(
                refreshDto,
                new ValidationContext(refreshDto),
                validationResults,
                validateAllProperties: true
            );

            if (!isValid)
            {
                logger.LogWarning($"Ошибка валидации при попытке входа: {validationResults}");
                var errors = validationResults
                    .GroupBy(v => v.MemberNames.FirstOrDefault() ?? "")
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(v => v.ErrorMessage ?? "Неизвестная ошибка").ToArray()
                    );
                return Results.ValidationProblem(errors);
            }

            var response = await service.Logout(refreshDto);

            return Results.Ok(response);
        }

        private static async Task<IResult> Refresh(
            RefreshDto refreshDto,
            IAuthService service,
            ILogger<IAuthService> logger)
        {
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(
                refreshDto,
                new ValidationContext(refreshDto),
                validationResults,
                validateAllProperties: true
            );

            if (!isValid)
            {
                logger.LogWarning($"Ошибка валидации при попытке входа: {validationResults}");
                var errors = validationResults
                    .GroupBy(v => v.MemberNames.FirstOrDefault() ?? "")
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(v => v.ErrorMessage ?? "Неизвестная ошибка").ToArray()
                    );
                return Results.ValidationProblem(errors);
            }

            var response = await service.Refresh(refreshDto);

            return Results.Ok(response);
        }
    }
}

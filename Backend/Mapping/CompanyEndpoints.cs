using Backend.Models;
using Backend.Models.Dto;
using Backend.Services;
using Backend.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;


namespace Backend.Mapping
{
    public static class CompanyEndpoints
    {

        public static void MapCompanyEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/companies");
            group.MapPost("/create", Create);
            group.MapGet("/review", Review);
            group.MapGet("account", Account);
            group.MapGet("/filterMap", FilterMap);
            group.MapGet("/search", Search);
            group.MapPut("/update", Update);
        }

        private static async Task<IResult> Create(
            [FromBody] CompanyCreateDto companyDto,
            [FromServices] CompanyService service,
            [FromServices] Logger<CompanyService> logger)
        {
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(
                companyDto,
                new ValidationContext(companyDto),
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
            var company = await service.CreateCompanyAsync(companyDto);
            if (company == null)
            {
                return Results.BadRequest("Регистрация не успешна");
            }
            return Results.Ok(company);
        }

        private static async Task<IResult> Review(
            [FromServices] CompanyService service)
        {
            var company = await service.ReviewCompanyAsync();
            var count = await service.CountCompaniesAsync();
            return Results.Ok(new { Count = count, Companies = company });
        }

        [Authorize]
        public static async Task<IResult> Account([FromQuery] Guid? id,
            [FromServices] CompanyService service,
            [FromServices] Logger<CompanyService> logger)
        {
            if (id == null)
            {
                logger.LogWarning("Id компании отсутствует");
                return Results.BadRequest("Id пуст");
            }
            var company = await service.AccountCompanyAsync(id);
            return Results.Ok(company);
        }

        [Authorize]
        private static async Task<IResult> Search(
            [FromQuery] string? industry,
            [FromQuery] string? region,
            [FromQuery] string? searchTerm,
            [FromServices] CompanyService service)
        {
            return await service.SearchCompanyAsync(industry, region, searchTerm);
        }
        public static async Task<IResult> FilterMap(
            [FromQuery] string? industries,
            [FromServices] CompanyService service)
        {
            //var addresses = await service.FilterMapCompanyAsync(industries);

            return Results.Ok();
        }
        [Authorize]
        public static async Task<IResult> Update([FromQuery] Guid? id,
            [FromBody] CompanyChangeDto companyDto,
            [FromServices] CompanyService service,
            [FromServices] Logger<CompanyService> logger)
        {
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(
                companyDto,
                new ValidationContext(companyDto),
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

            if (id == null)
            {
                logger.LogWarning("Id компании отсутствует");
                return Results.BadRequest("Id пуст");
            }
            var company = await service.UpdateCompanyAsync(id, companyDto);

            return Results.Ok(company);
        }
    }
}
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
            var group = app.MapGroup("/companies").WithTags("Companies");
            group.MapPost("/create", Create)
                .Produces<CompanyResponseDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest);
            group.MapGet("/review", Review)
                 .Produces<ReviewDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest);
            group.MapGet("/account", Account)
                .Produces<CompanyResponseDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest);
            group.MapGet("/filterMap", FilterMap);
            group.MapGet("/search", Search)
                .Produces<List<CompanyResponseDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest);
            group.MapPut("/update/{id}", Update)
                .Produces<CompanyResponseDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest);
        }

        private static async Task<IResult> Create(
            [FromBody] CompanyCreateDto companyDto,
            [FromServices] ICompanyService service,
            [FromServices] ILogger<CompanyService> logger)
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
            [FromServices] ICompanyService service)
        {
            var companies = await service.ReviewCompanyAsync();
            var count = await service.CountCompaniesAsync();

            var response = new ReviewDto { Count = count, Companies = companies };

            return Results.Ok(response);
        }

        public static async Task<IResult> Account(Guid? id,
            [FromServices] ICompanyService service,
            [FromServices] ILogger<CompanyService> logger)
        {
            if (id == null)
            {
                logger.LogError("Id компании отсутствует");
                return Results.BadRequest("Id компании отсутствует");
            }
            var company = await service.AccountCompanyAsync(id);
            return Results.Ok(company);
        }

        private static async Task<IResult> Search(
            [FromQuery] string? industry,
            [FromQuery] string? region,
            [FromQuery] string? searchTerm,
            [FromServices] ICompanyService service)
        {
            var companies = await service.SearchCompanyAsync(industry, region, searchTerm);

            return Results.Ok(companies);
        }
        public static async Task<IResult> FilterMap(
            [FromQuery] string? industries,
            [FromServices] ICompanyService service)
        {
            //var addresses = await service.FilterMapCompanyAsync(industries);

            return Results.Ok();
        }

        public static async Task<IResult> Update(Guid id,
            [FromBody] CompanyChangeDto companyDto,
            [FromServices] ICompanyService service,
            [FromServices] ILogger<CompanyService> logger)
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

            var company = await service.UpdateCompanyAsync(id, companyDto);

            return Results.Ok(company);
        }
    }
}
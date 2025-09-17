using Backend.Models;
using Backend.Models.Dto;
using Backend.Services;
using Backend.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Backend.Mapping
{
    public static class ManagerEndpoints
    {
        public static void MapManagerEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/managers");
            group.MapPost("/create", Create);
            group.MapGet("account", Account);
            group.MapPut("/update", Update);
        }

        private static async Task<IResult> Create(
            [FromBody] ManagerCreateDto managerDto,
            ManagerService service,
            Logger<ManagerService> logger)
        {
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(
                managerDto,
                new ValidationContext(managerDto),
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

            if (managerDto.Password.ToLower().Contains("script"))
            {
                logger.LogWarning("Попытка зарегистрировать опасный контент");
                throw new UnsafeContentException("Попытка зарегистрировать опасный контент");
            }

            managerDto.Name = managerDto.Name.Trim();
            managerDto.Password = managerDto.Password.Trim();
            managerDto.FullAddress = managerDto.FullAddress.Trim();
            managerDto.Email = managerDto.Email.Trim();
            managerDto.Phone = managerDto.Phone.Trim();

            var response = await service.CreateManagerAsync(managerDto);

            return Results.Ok(response);
        }

        [Authorize]
        public static async Task<IResult> Account(
            [FromQuery] Guid? id,
            ManagerService service,
            Logger<ManagerService> logger)
        {
            if (id == null)
            {
                logger.LogWarning("Id инвестора отсутствует");
                return Results.BadRequest("Id пуст");
            }

            var response = await service.AccountManagerAsync(id);

            return Results.Ok(response);
        }

        public static async Task<IResult> Update(
            [FromQuery] Guid? id,
            [FromBody] ManagerChangeDto managerDto,
            ManagerService service,
            Logger<ManagerService> logger)
        {
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(
                managerDto,
                new ValidationContext(managerDto),
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
                logger.LogWarning("Id инвестора отсутствует");
                return Results.BadRequest("Id пуст");
            }

            managerDto.Name = managerDto.Name.Trim();
            managerDto.FullAddress = managerDto.FullAddress.Trim();
            managerDto.Email = managerDto.Email.Trim();
            managerDto.Phone = managerDto.Phone.Trim();

            var response = await service.UpdateManagerAsync(id, managerDto);

            return Results.Ok(response);
        }
    }
}

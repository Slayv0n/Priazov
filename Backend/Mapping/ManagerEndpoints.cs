using Backend.Models.Dto;
using DataBase.Models;
using DataBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Dadata;
using Microsoft.Extensions.Options;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Mapping
{
    public static class ManagerEndpoints
    {
        private static readonly MemoryCacheEntryOptions CacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        public static void MapManagerEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/managers");
            group.MapPost("/create", Create);
            group.MapGet("account", Account);
            group.MapPut("/change", Change);
        }

        private static async Task<IResult> Create(
            [FromBody] ManagerCreateDto managerDto,
            [FromServices] IDbContextFactory<PriazovContext> factory,
            [FromServices] IOptions<DadataSettings> dadata,
            [FromServices] EmailService email,
            [FromServices] TurnstileService turnstile,
            [FromServices] ILogger<ManagerEndpointsLogger> logger)
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

            bool isHuman = await turnstile.VerifyTurnstileAsync(managerDto.Token);
            if (!isHuman)
            {
                logger.LogWarning($"Cloudflare Turnstile не пройдён для email: {managerDto.Email}");
                return Results.BadRequest("Проверка Cloudflare Turnstile не пройдена.");
            }

            managerDto.Name = managerDto.Name.Trim();
            managerDto.Password = managerDto.Password.Trim();
            managerDto.FullAddress = managerDto.FullAddress.Trim();
            managerDto.Email = managerDto.Email.Trim();
            managerDto.Phone = managerDto.Phone.Trim();

            if (managerDto.Password.ToLower().Contains("script"))
            {
                logger.LogWarning("Попытка зарегистрировать опасный контент");
                return Results.BadRequest();
            }

            using var db = await factory.CreateDbContextAsync();

            if (db.Users.Any(u => u.Email == managerDto.Email &&
            u.Address.FullAddress == managerDto.FullAddress))
            {
                logger.LogWarning($"Пользователь с таким email и адресом уже существует: {managerDto.Email}, {managerDto.FullAddress}");
                return Results.Conflict("Повтор уникальных данных");
            }

            var api = new CleanClientAsync(dadata.Value.ApiKey, dadata.Value.SecretKey);
            var cleanedAddress = await api.Clean<Dadata.Model.Address>(managerDto.FullAddress);

            if (cleanedAddress.result == null)
            {
                logger.LogWarning($"Адрес не найден: {managerDto.FullAddress}");
                return Results.NotFound("Адрес не найден");
            }

            var manager = new Manager()
            {
                Name = managerDto.Name,
                Email = managerDto.Email,
                Phone = managerDto.Phone,
                Address = new ShortAddressDto()
                {
                    FullAddress = cleanedAddress.result,
                    Latitude = decimal.Parse(cleanedAddress.geo_lat, CultureInfo.InvariantCulture),
                    Longitude = decimal.Parse(cleanedAddress.geo_lon, CultureInfo.InvariantCulture),
                }
            };

            await db.Users.AddAsync(manager);
            await db.SaveChangesAsync();

            await email.SendRegistrationEmail(manager);
            logger.LogInformation($"Инвестор зарегистрирована: {managerDto.Email}");

            return Results.Ok(new ManagerResponseDto(manager));
        }

        [Authorize]
        public static async Task<IResult> Account([FromQuery] Guid? id,
            [FromServices] IDbContextFactory<PriazovContext> factory,
            [FromServices] IMemoryCache cache,
            [FromServices] ILogger<ManagerEndpointsLogger> logger)
        {
            if (id == null)
            {
                logger.LogWarning("Id инвестора отсутствует");
                return Results.BadRequest("Id пуст");
            }

            var cacheKey = $"managers_{id}";
            if (cache.TryGetValue(cacheKey, out ManagerResponseDto? cachedManager))
            {
                logger.LogInformation($"Ответ взят из кэша: {cacheKey}");
                return Results.Ok(cachedManager);
            }
            else
            {
                logger.LogInformation($"Кэш промах. Запрос к БД: {cacheKey}");
            }

            using var db = await factory.CreateDbContextAsync();

            var manager = await db.Users.OfType<Manager>().Include(c => c.Address).FirstOrDefaultAsync(c => c.Id == id);

            if (manager == null)
            {
                logger.LogWarning($"Инвестор не найден по Id: {id}");
                return Results.NotFound();
            }

            var managerResponse = new ManagerResponseDto(manager);

            cache.Set(cacheKey, managerResponse, CacheOptions);
            logger.LogInformation($"Инвестор успешно найдена Id: {id}");

            return Results.Ok(managerResponse);
        }

        public static async Task<IResult> Change([FromQuery] Guid? id,
            [FromBody] ManagerChangeDto managerDto,
            [FromServices] IDbContextFactory<PriazovContext> factory,
            [FromServices] IOptions<DadataSettings> dadata,
            [FromServices] ILogger<ManagerEndpointsLogger> logger)
        {
            if (id == null)
            {
                logger.LogWarning("Id инвестора отсутствует");
                return Results.BadRequest("Id пуст");
            }

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

            managerDto.Name = managerDto.Name.Trim();
            managerDto.FullAddress = managerDto.FullAddress.Trim();
            managerDto.Email = managerDto.Email.Trim();
            managerDto.Phone = managerDto.Phone.Trim();

            using var db = await factory.CreateDbContextAsync();

            var manager = db.Users.OfType<Manager>().Include(c => c.Address).FirstOrDefault(c => c.Id == id);

            if (manager == null)
            {
                logger.LogWarning($"Инвестор не найден по Id: {id}");
                return Results.NotFound();
            }

            if (db.Users.Any(u => u.Email == managerDto.Email &&
            u.Address.FullAddress == managerDto.FullAddress && u.Id != id))
            {
                logger.LogWarning($"Пользователь с таким email и адресом уже существует: {managerDto.Email}, {managerDto.FullAddress}");
                return Results.Conflict("Повтор уникальных данных");
            }

            var api = new CleanClientAsync(dadata.Value.ApiKey, dadata.Value.SecretKey);
            var cleanedAddress = await api.Clean<Dadata.Model.Address>(managerDto.FullAddress);

            if (cleanedAddress.result == null)
            {
                logger.LogWarning($"Адрес не найден: {managerDto.FullAddress}");
                return Results.NotFound("Адрес не найден");
            }

            manager.Name = managerDto.Name;
            manager.Email = managerDto.Email;
            manager.Phone = managerDto.Phone;
            manager.PhotoIcon = managerDto.PhotoIcon;
            manager.Address = new ShortAddressDto()
            {
                FullAddress = cleanedAddress.result,
                Latitude = decimal.Parse(cleanedAddress.geo_lat, CultureInfo.InvariantCulture),
                Longitude = decimal.Parse(cleanedAddress.geo_lon, CultureInfo.InvariantCulture),
            };

            await db.SaveChangesAsync();
            logger.LogInformation($"Инвестор успешно изменен: {id}");

            return Results.Ok(new ManagerResponseDto(manager));
        }
    }
}

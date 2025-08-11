using Backend.Models;
using Backend.Models.Dto;
using Dadata;
using Dadata.Model;
using DataBase;
using DataBase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NLog.Config;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Backend.Services
{
    interface IManagerService
    {
        Task<IResult> CreateManagerAsync(ManagerCreateDto dto);
        Task<IResult> AccountManagerAsync(Guid? id);
        Task<IResult> UpdateManagerAsync(Guid? id, ManagerChangeDto dto);
        
    }

    public class ManagerService : IManagerService
    {
        private readonly MemoryCacheEntryOptions CacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        private readonly IDbContextFactory<PriazovContext> _factory;
        private readonly IOptions<DadataSettings> _dadata;
        private readonly IMessageService _email;
        private readonly TurnstileService _turnstile;
        private readonly ILogger<ManagerService> _logger;
        private readonly IMemoryCache _cache;

        public ManagerService(
            IDbContextFactory<PriazovContext> factory,
            IOptions<DadataSettings> dadata,
            IMessageService email,
            TurnstileService turnstile,
            ILogger<ManagerService> logger,
            IMemoryCache cache)
        {
            _factory = factory;
            _dadata = dadata;
            _email = email;
            _turnstile = turnstile;
            _logger = logger;
            _cache = cache;
        }
        public async Task<IResult> CreateManagerAsync(ManagerCreateDto managerDto)
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
                _logger.LogWarning($"Ошибка валидации при создании инвестора: {validationResults}");
                var errors = validationResults
                    .GroupBy(v => v.MemberNames.FirstOrDefault() ?? "")
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(v => v.ErrorMessage ?? "Неизвестная ошибка").ToArray()
                    );
                return Results.ValidationProblem(errors);
            }

            bool isHuman = await _turnstile.VerifyTurnstileAsync(managerDto.Token);
            if (!isHuman)
            {
                _logger.LogWarning($"Cloudflare Turnstile не пройдён для email: {managerDto.Email}");
                return Results.BadRequest("Проверка Cloudflare Turnstile не пройдена.");
            }

            managerDto.Name = managerDto.Name.Trim();
            managerDto.Password = managerDto.Password.Trim();
            managerDto.FullAddress = managerDto.FullAddress.Trim();
            managerDto.Email = managerDto.Email.Trim();
            managerDto.Phone = managerDto.Phone.Trim();

            if (managerDto.Password.ToLower().Contains("script"))
            {
                _logger.LogWarning("Попытка зарегистрировать опасный контент");
                return Results.BadRequest();
            }

            using var db = await _factory.CreateDbContextAsync();

            if (db.Users.Any(u => u.Email == managerDto.Email &&
            u.Address.FullAddress == managerDto.FullAddress))
            {
                _logger.LogWarning($"Пользователь с таким email и адресом уже существует: {managerDto.Email}, {managerDto.FullAddress}");
                return Results.Conflict("Повтор уникальных данных");
            }

            var api = new CleanClientAsync(_dadata.Value.ApiKey, _dadata.Value.SecretKey);
            var cleanedAddress = await api.Clean<Dadata.Model.Address>(managerDto.FullAddress);

            if (cleanedAddress.result == null)
            {
                _logger.LogWarning($"Адрес не найден: {managerDto.FullAddress}");
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

            await _email.SendRegistrationEmail(manager);
            _logger.LogInformation($"Инвестор зарегистрирована: {managerDto.Email}");

            return Results.Ok(new ManagerResponseDto(manager));
        }
        public async Task<IResult> AccountManagerAsync(Guid? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Id инвестора отсутствует");
                return Results.BadRequest("Id пуст");
            }

            var cacheKey = $"managers_{id}";
            if (_cache.TryGetValue(cacheKey, out ManagerResponseDto? cachedManager))
            {
                _logger.LogInformation($"Ответ взят из кэша: {cacheKey}");
                return Results.Ok(cachedManager);
            }
            else
            {
                _logger.LogInformation($"Кэш промах. Запрос к БД: {cacheKey}");
            }

            using var db = await _factory.CreateDbContextAsync();

            var manager = await db.Users.OfType<Manager>().Include(c => c.Address).FirstOrDefaultAsync(c => c.Id == id);

            if (manager == null)
            {
                _logger.LogWarning($"Инвестор не найден по Id: {id}");
                return Results.NotFound();
            }

            var managerResponse = new ManagerResponseDto(manager);

            _cache.Set(cacheKey, managerResponse, CacheOptions);
            _logger.LogInformation($"Инвестор успешно найдена Id: {id}");

            return Results.Ok(managerResponse);
        }

        public async Task<IResult> UpdateManagerAsync(Guid? id, ManagerChangeDto managerDto)
        {
            if (id == null)
            {
                _logger.LogWarning("Id инвестора отсутствует");
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
                _logger.LogWarning($"Ошибка валидации при создании инвестора: {validationResults}");
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

            using var db = await _factory.CreateDbContextAsync();

            var manager = db.Users.OfType<Manager>().Include(c => c.Address).FirstOrDefault(c => c.Id == id);

            if (manager == null)
            {
                _logger.LogWarning($"Инвестор не найден по Id: {id}");
                return Results.NotFound();
            }

            if (db.Users.Any(u => u.Email == managerDto.Email &&
            u.Address.FullAddress == managerDto.FullAddress && u.Id != id))
            {
                _logger.LogWarning($"Пользователь с таким email и адресом уже существует: {managerDto.Email}, {managerDto.FullAddress}");
                return Results.Conflict("Повтор уникальных данных");
            }

            var api = new CleanClientAsync(_dadata.Value.ApiKey, _dadata.Value.SecretKey);
            var cleanedAddress = await api.Clean<Dadata.Model.Address>(managerDto.FullAddress); 

            if (cleanedAddress.result == null)
            {
                _logger.LogWarning($"Адрес не найден: {managerDto.FullAddress}");
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
            _logger.LogInformation($"Инвестор успешно изменен: {id}");

            return Results.Ok(new ManagerResponseDto(manager));
        }
    }
}

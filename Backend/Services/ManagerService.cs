using Backend.Models;
using Backend.Models.Dto;
using Backend.Validation;
using Dadata;
using DataBase;
using DataBase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Globalization;

namespace Backend.Services
{
    public interface IManagerService
    {
        Task<ManagerResponseDto> CreateManagerAsync(ManagerCreateDto dto);
        Task<ManagerResponseDto> AccountManagerAsync(Guid? id);
        Task<ManagerResponseDto> UpdateManagerAsync(Guid? id, ManagerChangeDto dto);
    }

    public class ManagerService : IManagerService
    {
        private CancellationTokenSource _cacheResetToken = new();

        private MemoryCacheEntryOptions CreateCacheOptions(TimeSpan? expiration = null)
        {
            return new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
            }
            .AddExpirationToken(new CancellationChangeToken(_cacheResetToken.Token));
        }

        public void ResetManagersCache()
        {
            _cacheResetToken.Cancel();
            _cacheResetToken = new CancellationTokenSource();
        }

        private readonly IDbContextFactory<PriazovContext> _factory;
        private readonly IOptions<DadataSettings> _dadata;
        private readonly IMessageService _message;
        private readonly ILogger<ManagerService> _logger;
        private readonly IMemoryCache _cache;

        public ManagerService(
            IDbContextFactory<PriazovContext> factory,
            IOptions<DadataSettings> dadata,
            IMessageService message,
            ILogger<ManagerService> logger,
            IMemoryCache cache)
        {
            _factory = factory;
            _dadata = dadata;
            _message = message;
            _logger = logger;
            _cache = cache;
        }
        public async Task<ManagerResponseDto> CreateManagerAsync(ManagerCreateDto managerDto)
        {

            using var db = await _factory.CreateDbContextAsync();

            if (db.Users.Any(u => u.Email == managerDto.Email))
            {
                _logger.LogWarning($"Пользователь с таким email уже сущетсвует: {managerDto.Email}");
                throw new ConflictException("Повтор уникальных данных");
            }

            var api = new CleanClientAsync(_dadata.Value.ApiKey, _dadata.Value.SecretKey);
            var cleanedAddress = await api.Clean<Dadata.Model.Address>(managerDto.FullAddress);

            if (cleanedAddress.result == null)
            {
                _logger.LogWarning($"Адрес не найден: {managerDto.FullAddress}");
                throw new NotFoundException("Адрес не найден");
            }

            var manager = new Manager()
            {
                Name = managerDto.Name,
                Email = managerDto.Email,
                Phone = managerDto.Phone,
                Address = new ShortAddressDto()
                {
                    Region = cleanedAddress.region_with_type,
                    FullAddress = cleanedAddress.result,
                    Latitude = decimal.Parse(cleanedAddress.geo_lat, CultureInfo.InvariantCulture),
                    Longitude = decimal.Parse(cleanedAddress.geo_lon, CultureInfo.InvariantCulture),
                }
            };

            await db.Users.AddAsync(manager);
            await db.SaveChangesAsync();

            await _message.SendRegistrationEmail(manager);
            _logger.LogInformation($"Инвестор зарегистрирована: {managerDto.Email}");

            ResetManagersCache();

            return new ManagerResponseDto(manager);
        }
        public async Task<ManagerResponseDto> AccountManagerAsync(Guid? id)
        {
            var cacheKey = $"managers_{id}";
            if (_cache.TryGetValue(cacheKey, out ManagerResponseDto? cachedManager))
            {
                _logger.LogInformation($"Ответ взят из кэша: {cacheKey}");
                return cachedManager!;
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
                throw new NotFoundException("Инвестор не найден по Id");
            }

            var managerResponse = new ManagerResponseDto(manager);

            _cache.Set(cacheKey, managerResponse, CreateCacheOptions(TimeSpan.FromMinutes(30)));
            _logger.LogInformation($"Инвестор успешно найдена Id: {id}");

            return managerResponse;
        }

        public async Task<ManagerResponseDto> UpdateManagerAsync(Guid? id, ManagerChangeDto managerDto)
        {
            using var db = await _factory.CreateDbContextAsync();

            var manager = db.Users.OfType<Manager>().Include(c => c.Address).FirstOrDefault(c => c.Id == id);

            if (manager == null)
            {
                _logger.LogWarning($"Инвестор не найден по Id: {id}");
                throw new NotFoundException("Инвестор не найден по Id");
            }

            if (db.Users.Any(u => u.Email == managerDto.Email && u.Id != id))
            {
                _logger.LogWarning($"Пользователь с таким email уже существует: {managerDto.Email}");
                throw new ConflictException("Повтор уникальных данных");
            }

            manager.Name = managerDto.Name;
            manager.Email = managerDto.Email;
            manager.Phone = managerDto.Phone;
            manager.AvatarId = managerDto.AvatarId;

            await db.SaveChangesAsync();
            _logger.LogInformation($"Инвестор успешно изменен: {id}");

            ResetManagersCache();

            return new ManagerResponseDto(manager);
        }
    }
}

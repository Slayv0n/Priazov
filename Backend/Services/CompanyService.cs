using Backend.Models;
using Backend.Models.Dto;
using Dadata.Model;
using Dadata;
using DataBase;
using DataBase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NLog.Config;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using System.Text;
using Backend.Validation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Backend.Services
{
    public interface ICompanyService
    {
        Task<CompanyResponseDto> CreateCompanyAsync(CompanyCreateDto companyDto);
        Task<CompanyResponseDto> AccountCompanyAsync(Guid? id);
        Task<List<CompanyResponseDto>> ReviewCompanyAsync();
        Task<int> CountCompaniesAsync();
        Task<IResult> SearchCompanyAsync(string? industry, string? region, string? searchTerm);
        Task<List<AddressDto>> FilterMapCompanyAsync(List<Filter> industries);
        Task<IResult> UpdateCompanyAsync(Guid? id, CompanyChangeDto companyDto);
    }

    public class CompanyService : ICompanyService
    {
        private readonly HashSet<string> _allowedRegions = new()
        {
            "Ростовская область",
            "Краснодарский край",
            "ЛНР",
            "ДНР",
            "Республика Крым",
            "Херсонская область",
            "Запорожская область"
        };
        private readonly MemoryCacheEntryOptions CacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        private readonly IDbContextFactory<PriazovContext> _factory;
        private readonly DadataSettings _dadata;
        private readonly IMessageService _messageService;
        private readonly ILogger<CompanyService> _logger;
        private readonly IMemoryCache _cache;

        public CompanyService(
            IDbContextFactory<PriazovContext> factory,
            IOptions<DadataSettings> dadata,
            IMessageService messageService,
            ILogger<CompanyService> logger,
            IMemoryCache cache)
        {
            _factory = factory;
            _dadata = dadata.Value;
            _messageService = messageService;
            _logger = logger;
            _cache = cache;
        }

        public async Task<CompanyResponseDto> CreateCompanyAsync(CompanyCreateDto companyDto)
        {
            companyDto.Name = companyDto.Name.Trim();
            companyDto.Password = companyDto.Password.Trim();
            companyDto.FullAddress = companyDto.FullAddress.Trim();
            companyDto.Email = companyDto.Email.Trim();
            companyDto.Phone = companyDto.Phone.Trim();
            companyDto.Industry = companyDto.Industry.Trim();
            companyDto.LeaderName = companyDto.LeaderName.Trim();

            if (companyDto.Password.ToLower().Contains("script"))
            {
                _logger.LogWarning("Попытка зарегистрировать опасный контент");
                throw new UnsafeContentException("Попытка зарегистрировать опасный контент");
            }

            using var db = await _factory.CreateDbContextAsync();

            if (db.Users.Any(u => u.Email == companyDto.Email &&
            u.Address.FullAddress == companyDto.FullAddress))
            {
                _logger.LogWarning($"Пользователь с таким email и адресом уже существует: {companyDto.Email}, {companyDto.FullAddress}");
                throw new ConflictException("Повтор уникальных данных");
            }

            var api = new CleanClientAsync(_dadata.ApiKey, _dadata.SecretKey);
            var cleanedAddress = await api.Clean<Dadata.Model.Address>(companyDto.FullAddress);

            if (cleanedAddress.result == null || cleanedAddress.geo_lat == null || cleanedAddress.geo_lon == null)
            {
                _logger.LogWarning($"Адрес не найден: {companyDto.FullAddress}");
                throw new NotFoundException("Адрес не найден");
            }

            var company = new Company()
            {
                Name = companyDto.Name,
                Email = companyDto.Email,
                Password = new UserPassword()
                {
                    PasswordHash = PasswordHasher.HashPassword(companyDto.Password.Trim()),
                    LastUpdated = DateTime.UtcNow
                },
                Phone = companyDto.Phone,
                Address = new ShortAddressDto()
                {
                    FullAddress = cleanedAddress.result,
                    Latitude = decimal.Parse(cleanedAddress.geo_lat, CultureInfo.InvariantCulture),
                    Longitude = decimal.Parse(cleanedAddress.geo_lon, CultureInfo.InvariantCulture)
                },
                Industry = companyDto.Industry,
                LeaderName = companyDto.LeaderName
            };
            await db.Users.AddAsync(company);
            await db.SaveChangesAsync();

            await _messageService.SendRegistrationEmail(company);
            _logger.LogInformation($"Компания зарегистрирована: {companyDto.Email}");

            return new CompanyResponseDto(company, company.Address.FullAddress);
        }

        public async Task<CompanyResponseDto> AccountCompanyAsync(Guid? id)
        {
            var cacheKey = $"companies_{id}";
            if (_cache.TryGetValue(cacheKey, out CompanyResponseDto? cachedCompany))
            {
                _logger.LogInformation($"Ответ взят из кэша: {cacheKey}");
                return cachedCompany!;
            }
            else
            {
                _logger.LogInformation($"Кэш промах. Запрос к БД: {cacheKey}");
            }

            using var db = await _factory.CreateDbContextAsync();

            var company = await db.Users.OfType<Company>().Include(c => c.Address).FirstOrDefaultAsync(c => c.Id == id);

            if (company == null)
            {
                _logger.LogWarning($"Компания не найдена по Id: {id}");
                throw new NotFoundException("Компания не найдена");
            }

            var companyResponse = new CompanyResponseDto(company, company.Address.FullAddress);

            _cache.Set(cacheKey, companyResponse, CacheOptions);
            _logger.LogInformation($"Компания успешно найдена Id: {id}");

            return companyResponse;
        }

        public async Task<List<CompanyResponseDto>> ReviewCompanyAsync()
        {
            var cacheKey = $"companies_review";

            if (_cache.TryGetValue(cacheKey, out List<CompanyResponseDto>? cachedCompanies))
            {
                _logger.LogInformation($"Ответ взят из кэша: {cacheKey}");
                return cachedCompanies!; 
            }
            else
            {
                _logger.LogInformation($"Кэш промах. Запрос к БД: {cacheKey}");
            }

            using var db = await _factory.CreateDbContextAsync();

            var query = await db.Users.OfType<Company>()
                .AsQueryable()
                .OrderBy(c => c.Name)
                .Take(5)
                .Include(c => c.Address)
                .Select(c => new CompanyResponseDto(c, c.Address.FullAddress))
                .ToListAsync();

            _cache.Set(cacheKey, query, CacheOptions);
            _logger.LogInformation("Краткий просмотр компаний выполнен");

            return query;
        }

        public async Task<int> CountCompaniesAsync()
        {
            using var db = await _factory.CreateDbContextAsync();
            return await db.Users.OfType<Company>().CountAsync();
        }

        public async Task<IResult> SearchCompanyAsync(string? industry, string? region, string? searchTerm)
        {
            var cacheKey = $"companies_search_{industry ?? "all"}_{region ?? "all"}_{searchTerm ?? "all"}";

            if (_cache.TryGetValue(cacheKey, out List<CompanyResponseDto>? cachedCompanies))
            {
                _logger.LogInformation($"Ответ взят из кэша: {cacheKey}");
                return Results.Ok(cachedCompanies);
            }
            else
            {
                _logger.LogInformation($"Кэш промах. Запрос к БД: {cacheKey}");
            }

            if (region != null && !_allowedRegions.Contains(region))
            {
                _logger.LogInformation($"Недопустимое значение региона: {region}");
                return Results.BadRequest("Недопустимые значения региона");
            }

            using var db = await _factory.CreateDbContextAsync();

            var query = db.Users.OfType<Company>().AsQueryable().Include(c => c.Address).Where(c => c.Industry == industry);

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(c => EF.Functions.ILike(c.Name, $"%{searchTerm}%"));

            var companies = await query.OrderBy(c => c.Name).Select(c => new CompanyResponseDto(c, c.Address.FullAddress)).ToListAsync();

            _cache.Set(cacheKey, companies, CacheOptions);
            _logger.LogInformation("Поиск и фильтрация компаний завершились успешно");

            return Results.Ok(companies);
        }

        public async Task<List<AddressDto>> FilterMapCompanyAsync(List<Filter>? industries)
        {
            StringBuilder key = new StringBuilder();

            if (industries != null && industries.Any(i => i.IsChecked))
            {
                foreach (Filter filter in industries)
                {
                    if (filter.IsChecked)
                    {
                        key.Append(filter.Industry);
                    }
                }
            }
            
            var cacheKey = $"companies_filterMap_{key.ToString() ?? "all"}";

            if (_cache.TryGetValue(cacheKey, out List<AddressDto>? cachedAddress))
            {
                _logger.LogInformation($"Ответ взят из кэша: {cacheKey}");
                return cachedAddress!;
            }
            else
            {
                _logger.LogInformation($"Кэш промах. Запрос к БД: {cacheKey}");
            }

            using var db = await _factory.CreateDbContextAsync();

            var companies = db.Users.OfType<Company>();

            if (industries != null && industries.Any(i => i.IsChecked))
            {
                List<string>? industryList = industries.Where(filter => filter.IsChecked)
                .Select(filter => filter.Industry)
                .ToList();

                companies = companies.Where(c => industryList.Contains(c.Industry));
            }
                
            var addresses = companies
                .Include(c => c.Address)
                .AsEnumerable()
                .GroupBy(c => c.Address.FullAddress)
                .Select(g => new AddressDto
                {
                    Address = g.First().Address,
                    Companies = g.Select(c => new CompanyResponseDto(c, c.Address.FullAddress)).ToList()
                })
                .ToList();

            _cache.Set(cacheKey, addresses, CacheOptions);
            _logger.LogInformation("Фильтрация адресов завершилась успешно");

            return addresses;
        }

        public async Task<IResult> UpdateCompanyAsync(Guid? id, CompanyChangeDto companyDto)
        {
            if (id == null)
            {
                _logger.LogWarning("Id компании отсутствует");
                return Results.BadRequest("Id пуст");
            }

            companyDto.Name = companyDto.Name.Trim();
            companyDto.FullAddress = companyDto.FullAddress.Trim();
            companyDto.Email = companyDto.Email.Trim();
            companyDto.Phone = companyDto.Phone.Trim();
            companyDto.Industry = companyDto.Industry.Trim();
            companyDto.LeaderName = companyDto.LeaderName.Trim();
            companyDto.Description = companyDto.Description?.Trim();

            using var db = await _factory.CreateDbContextAsync();

            var company = db.Users.OfType<Company>().Include(c => c.Address).FirstOrDefault(c => c.Id == id);

            if (company == null)
            {
                _logger.LogWarning($"Компания не найдена по Id: {id}");
                return Results.NotFound();
            }

            if (db.Users.Any(u => u.Email == companyDto.Email &&
            u.Address.FullAddress == companyDto.FullAddress && u.Id != id))
            {
                _logger.LogWarning($"Пользователь с таким email и адресом уже существует: {companyDto.Email}, {companyDto.FullAddress}");
                return Results.Conflict("Повтор уникальных данных");
            }

            var api = new CleanClientAsync(_dadata.ApiKey, _dadata.SecretKey);
            var cleanedAddress = await api.Clean<Dadata.Model.Address>(companyDto.FullAddress);

            if (cleanedAddress.result == null)
            {
                _logger.LogWarning($"Адрес не найден: {companyDto.FullAddress}");
                return Results.NotFound("Адрес не найден");
            }

            company.Name = companyDto.Name;
            company.Email = companyDto.Email;
            company.Phone = companyDto.Phone;
            company.Industry = companyDto.Industry;
            company.PhotoIcon = companyDto.PhotoIcon;
            company.PhotoHeader = companyDto.PhotoHeader;
            company.Address = new ShortAddressDto()
            {
                FullAddress = cleanedAddress.result,
                Latitude = decimal.Parse(cleanedAddress.geo_lat, CultureInfo.InvariantCulture),
                Longitude = decimal.Parse(cleanedAddress.geo_lon, CultureInfo.InvariantCulture)
            };
            company.Contacts.VirtualList = companyDto.Contacts.VirtualList.Select(i => i.Trim()).ToList();
            company.LeaderName = companyDto.LeaderName;
            company.Description = companyDto.Description;

            await db.SaveChangesAsync();
            _logger.LogInformation($"Компания успешно изменена: {id}");

            return Results.Ok(new CompanyResponseDto(company, company.Address.FullAddress));
        }
    }
}

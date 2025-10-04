using Backend.Models;
using Backend.Models.Dto;
using Backend.Validation;
using Dadata;
using Dadata.Model;
using DataBase;
using DataBase.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using NLog.Config;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Backend.Services
{
    public interface ICompanyService
    {
        Task<CompanyResponseDto> CreateCompanyAsync(CompanyCreateDto companyDto);
        Task<CompanyResponseDto> AccountCompanyAsync(Guid? id);
        Task<List<CompanyResponseDto>> ReviewCompanyAsync();
        Task<int> CountCompaniesAsync();
        Task<List<CompanyResponseDto>> SearchCompanyAsync(string? searchTerm, string? industry, string? region);
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

        private readonly IDbContextFactory<PriazovContext> _factory;
        private readonly DadataSettings _dadata;
        private readonly IMessageService _messageService;
        private readonly ILogger<CompanyService> _logger;
        private readonly ICacheService _cacheService;
        private readonly string _cacheName = "companies_";


        public CompanyService(
            IDbContextFactory<PriazovContext> factory,
            IOptions<DadataSettings> dadata,
            IMessageService messageService,
            ILogger<CompanyService> logger,
            ICacheService cacheService)
        {
            _factory = factory;
            _dadata = dadata.Value;
            _messageService = messageService;
            _logger = logger;
            _cacheService = cacheService;
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
                _logger.LogError("Попытка зарегистрировать опасный контент");
                throw new UnsafeContentException("Попытка зарегистрировать опасный контент");
            }

            using var db = await _factory.CreateDbContextAsync();

            if (db.Users.Any(u => u.Email == companyDto.Email &&
            u.Address.FullAddress == companyDto.FullAddress))
            {
                _logger.LogError($"Пользователь с таким email и адресом уже существует: {companyDto.Email}, {companyDto.FullAddress}");
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

            _cacheService.ResetCache(_cacheName);

            return new CompanyResponseDto(company, company.Address.FullAddress);
        }

        public async Task<CompanyResponseDto> AccountCompanyAsync(Guid? id)
        {
            var cacheKey = $"{_cacheName}_{id}";
            if (_cacheService.GetCache().TryGetValue(cacheKey, out CompanyResponseDto? cachedCompany))
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

            _cacheService.GetCache().Set(cacheKey, companyResponse, _cacheService.CreateCacheOptions(TimeSpan.FromMinutes(30)));
            _cacheService.SetCacheKeys(cacheKey);

            _logger.LogInformation($"Компания успешно найдена Id: {id}");

            return companyResponse;
        }

        public async Task<List<CompanyResponseDto>> ReviewCompanyAsync()
        {
            var cacheKey = $"{_cacheName}_review";

            if (_cacheService.GetCache().TryGetValue(cacheKey, out List<CompanyResponseDto>? cachedCompanies))
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

            _cacheService.GetCache().Set(cacheKey, query, _cacheService.CreateCacheOptions(TimeSpan.FromMinutes(15)));
            _logger.LogInformation("Краткий просмотр компаний выполнен");

            return query;
        }

        public async Task<int> CountCompaniesAsync()
        {
            using var db = await _factory.CreateDbContextAsync();
            return await db.Users.OfType<Company>().CountAsync();
        }

        public async Task<List<CompanyResponseDto>> SearchCompanyAsync(string? searchTerm, string? industry, string? region)
        {
            var cacheKey = $"{_cacheName}_search_{industry ?? "all"}_{region ?? "all"}_{searchTerm ?? "all"}";

            if (_cacheService.GetCache().TryGetValue(cacheKey, out List<CompanyResponseDto>? cachedCompanies))
            {
                _logger.LogInformation($"Ответ взят из кэша: {cacheKey}");
                return cachedCompanies!;
            }
            else
            {
                _logger.LogInformation($"Кэш промах. Запрос к БД: {cacheKey}");
            }

            if (!string.IsNullOrEmpty(region) && !_allowedRegions.Contains(region))
            {
                _logger.LogError($"Недопустимое значение региона: {region}");
                throw new Exception("Недопустимые значения региона");
            }

            using var db = await _factory.CreateDbContextAsync();

            var query = db.Users.OfType<Company>().AsQueryable().Include(c => c.Address).Where(c => c.Industry.Contains(industry ?? ""));

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(c => EF.Functions.ILike(c.Name, $"%{searchTerm}%"));

            var companies = await query.OrderBy(c => c.Name).Select(c => new CompanyResponseDto(c, c.Address.FullAddress)).ToListAsync();

            _cacheService.GetCache().Set(cacheKey, companies, _cacheService.CreateCacheOptions());
            _cacheService.SetCacheKeys(cacheKey);
            _logger.LogInformation("Поиск и фильтрация компаний завершились успешно");

            return companies;
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
            
            var cacheKey = $"{_cacheName}_filterMap_{key.ToString() ?? "all"}";

            if (_cacheService.GetCache().TryGetValue(cacheKey, out List<AddressDto>? cachedAddress))
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

            _cacheService.GetCache().Set(cacheKey, addresses, _cacheService.CreateCacheOptions(TimeSpan.FromMinutes(15)));
            _cacheService.SetCacheKeys(cacheKey);
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

            company.Name = companyDto.Name;
            company.Email = companyDto.Email;
            company.Phone = companyDto.Phone;
            company.Industry = companyDto.Industry;
            company.Contacts.VirtualList = companyDto.Contacts.VirtualList.Select(i => i.Trim()).ToList();
            company.LeaderName = companyDto.LeaderName;
            company.Description = companyDto.Description;

            await db.SaveChangesAsync();
            _logger.LogInformation($"Компания успешно изменена: {id}");

            _cacheService.ResetCache(_cacheName);

            return Results.Ok(new CompanyResponseDto(company, company.Address.FullAddress));
        }
    }
}

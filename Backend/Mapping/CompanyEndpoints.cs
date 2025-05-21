using Backend.Models;
using Backend.Models.Dto;
using Backend.Validation;
using Dadata;
using Dadata.Model;
using DataBase;
using DataBase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Org.BouncyCastle.Asn1.Ocsp;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Backend.Mapping
{
    public static class CompanyEndpoints
    {
        private static readonly HashSet<string> _allowedIndustries = new()
        {
            "Образовательное учреждение",
            "Научно-исследовательский институт",
            "Научно-образовательный проект",
            "Государственное учреждение",
            "Коммерческая деятельность",
            "Стартап",
            "Финансы",
            "Акселератор / инкубатор / технопарк",
            "Ассоциация / объединение",
            "Инициатива",
            "Отраслевое событие / научная конференция",
            "Государственное учреждение",
            "Некоммерческая организация",
            "Другое"
        };
        private static readonly HashSet<string> _allowedRegions = new()
        {
            "Ростовская область",
            "Краснодарский край",
            "ЛНР",
            "ДНР",
            "Республика Крым",
            "Херсонская область",
            "Запорожская область"
        };
        private static readonly MemoryCacheEntryOptions CacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };


        public static void MapCompanyEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/companies");
            group.MapPost("/create", Create);
            group.MapGet("/review", Review);
            group.MapGet("account", Account);
            group.MapGet("/filterMap", FilterMap);
            group.MapGet("/search", SearchCompanies);
            group.MapPut("/change", Change);
        }

        private static async Task<IResult> Create(
            [FromBody] CompanyCreateDto companyDto,
            [FromServices] IDbContextFactory<PriazovContext> factory,
            [FromServices] IOptions<DadataSettings> dadata,
            [FromServices] EmailService email,
            [FromServices] TurnstileService turnstile,
            [FromServices] ILogger<CompanyEndpointsLogger> logger)
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
                logger.LogWarning($"Ошибка валидации при создании компании: {validationResults}");
                var errors = validationResults
                    .GroupBy(v => v.MemberNames.FirstOrDefault() ?? "")
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(v => v.ErrorMessage ?? "Неизвестная ошибка").ToArray()
                    );
                return Results.ValidationProblem(errors);
            }

            bool isHuman = await turnstile.VerifyTurnstileAsync(companyDto.Token);
            if (!isHuman)
            {
                logger.LogWarning($"Cloudflare Turnstile не пройдён для email: {companyDto.Email}");
                return Results.BadRequest("Проверка Cloudflare Turnstile не пройдена.");
            }
                
            companyDto.Name = companyDto.Name.Trim();
            companyDto.Password = companyDto.Password.Trim();
            companyDto.FullAddress = companyDto.FullAddress.Trim();
            companyDto.Email = companyDto.Email.Trim();
            companyDto.Phone = companyDto.Phone.Trim();
            companyDto.Industry = companyDto.Industry.Trim();
            companyDto.LeaderName = companyDto.LeaderName.Trim();

            if (!_allowedIndustries.Any(i => i == companyDto.Industry))
            {
                logger.LogWarning($"Индустрия не найдена в списке: {companyDto.Industry}");
                return Results.BadRequest("Недопустимое значение индустрии");
            }
                
            if (companyDto.Password.ToLower().Contains("script"))
            {
                logger.LogWarning("Попытка зарегистрировать опасный контент");
                return Results.BadRequest();
            }

            using var db = await factory.CreateDbContextAsync();

            if (db.Users.Any(u => u.Email == companyDto.Email &&
            u.Address.FullAddress == companyDto.FullAddress))
            {
                logger.LogWarning($"Пользователь с таким email и адресом уже существует: {companyDto.Email}, {companyDto.FullAddress}");
                return Results.Conflict("Повтор уникальных данных");
            }
                
            var api = new CleanClientAsync(dadata.Value.ApiKey, dadata.Value.SecretKey);
            var cleanedAddress = await api.Clean<Dadata.Model.Address>(companyDto.FullAddress);

            if (cleanedAddress.result == null)
            {
                logger.LogWarning($"Адрес не найден: {companyDto.FullAddress}");
                return Results.NotFound("Адрес не найден");
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

            await email.SendRegistrationEmail(company);
            logger.LogInformation($"Компания зарегистрирована: {companyDto.Email}");

            return Results.Ok(new CompanyResponseDto(company, company.Address.FullAddress));
        }

        private static async Task<IResult> Review(
            [FromServices] IDbContextFactory<PriazovContext> factory,
            [FromServices] IMemoryCache cache,
            [FromServices] ILogger<CompanyEndpointsLogger> logger)
        {
            var cacheKey = $"companies_review";

            if (cache.TryGetValue(cacheKey, out List<Company>? cachedCompanies))
            {
                logger.LogInformation($"Ответ взят из кэша: {cacheKey}");
                return Results.Ok(cachedCompanies);
            }
            else
            {
                logger.LogInformation($"Кэш промах. Запрос к БД: {cacheKey}");
            }

            using var db = await factory.CreateDbContextAsync();
            
            var query = await db.Users.OfType<Company>()
                .AsQueryable()
                .OrderBy(c => c.Name)
                .Take(5)
                .Include(c => c.Address)
                .Select(c => new CompanyResponseDto(c, c.Address.FullAddress))
                .ToListAsync();

            var count = await db.Users.OfType<Company>().CountAsync() - query.Count;

            cache.Set(cacheKey, query, CacheOptions);
            logger.LogInformation("Краткий просмотр компаний выполнен");

            return Results.Ok(new { query, count });
        }

        [Authorize]
        public static async Task<IResult> Account([FromQuery] Guid? id,
            [FromServices] IDbContextFactory<PriazovContext> factory,
            [FromServices] IMemoryCache cache,
            [FromServices] ILogger<CompanyEndpointsLogger> logger)
        {
            if (id == null)
            {
                logger.LogWarning("Id компании отсутствует");
                return Results.BadRequest("Id пуст");
            }             

            var cacheKey = $"companies_{id}";
            if (cache.TryGetValue(cacheKey, out CompanyResponseDto? cachedCompany))
            {
                logger.LogInformation($"Ответ взят из кэша: {cacheKey}");
                return Results.Ok(cachedCompany);
            }
            else
            {
                logger.LogInformation($"Кэш промах. Запрос к БД: {cacheKey}");
            }

            using var db = await factory.CreateDbContextAsync();

            var company = await db.Users.OfType<Company>().Include(c => c.Address).FirstOrDefaultAsync(c => c.Id == id);

            if (company == null)
            {
                logger.LogWarning($"Компания не найдена по Id: {id}");
                return Results.NotFound();
            }
                
            var companyResponse = new CompanyResponseDto(company, company.Address.FullAddress);

            cache.Set(cacheKey, companyResponse, CacheOptions);
            logger.LogInformation($"Компания успешно найдена Id: {id}");

            return Results.Ok(companyResponse);
        }

        [Authorize]
        private static async Task<IResult> SearchCompanies(
            [FromQuery] string? industry,
            [FromQuery] string? region,
            [FromQuery] string? searchTerm,
            [FromServices] IDbContextFactory<PriazovContext> factory,
            [FromServices] IMemoryCache cache,
            [FromServices] ILogger<CompanyEndpointsLogger> logger)
        {
            var cacheKey = $"companies_search_{industry ?? "all"}_{region ?? "all"}_{searchTerm ?? "all"}";

            if (cache.TryGetValue(cacheKey, out List<CompanyResponseDto>? cachedCompanies))
            {
                logger.LogInformation($"Ответ взят из кэша: {cacheKey}");
                return Results.Ok(cachedCompanies);
            }
            else
            {
                logger.LogInformation($"Кэш промах. Запрос к БД: {cacheKey}");
            }

            if (industry != null && !_allowedIndustries.Contains(industry))
            {
                logger.LogInformation($"Недопустимое значение индустрии: {industry}");
                return Results.BadRequest("Недопустимое значение индустрии");
            }
                

            if (region != null && !_allowedRegions.Contains(region))
            {
                logger.LogInformation($"Недопустимое значение региона: {region}");
                return Results.BadRequest("Недопустимые значения региона");
            }       

            using var db = await factory.CreateDbContextAsync();

            var query = db.Users.OfType<Company>().AsQueryable().Include(c => c.Address).Where(c => c.Industry == industry);

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(c => EF.Functions.ILike(c.Name, $"%{searchTerm}%"));

            var companies = await query.OrderBy(c => c.Name).Select(c => new CompanyResponseDto(c, c.Address.FullAddress)).ToListAsync();

            cache.Set(cacheKey, companies, CacheOptions);
            logger.LogInformation("Поиск и фильтрация компаний завершились успешно");

            return Results.Ok(companies);
        }
        public static async Task<IResult> FilterMap(
                    [FromQuery] string? industries,
                    [FromServices] IDbContextFactory<PriazovContext> factory,
                    [FromServices] IMemoryCache cache,
                    [FromServices] ILogger<CompanyEndpointsLogger> logger)
        {
            var cacheKey = $"companies_filterMap_{industries ?? "all"}";

            if (cache.TryGetValue(cacheKey, out List<(ShortAddressDto Address, List<CompanyResponseDto> Companies)>? cachedAddress))
            {
                logger.LogInformation($"Ответ взят из кэша: {cacheKey}");
                return Results.Ok(cachedAddress);
            }
            else
            {
                logger.LogInformation($"Кэш промах. Запрос к БД: {cacheKey}");
            }

            List<string>? industryList = null;
            if (!string.IsNullOrEmpty(industries))
                industryList = industries.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(i => i.Trim())
                                        .ToList();

            if (industryList?.Count > 0 && industryList.Any(i => !_allowedIndustries.Contains(i)))
            {
                logger.LogInformation($"Недопустимые значения индустрий: {industryList}");
                return Results.BadRequest("Недопустимые значения индустрий.");
            }
                


            using var db = await factory.CreateDbContextAsync();

            var query = db.Users.OfType<Company>().Select(user => new
            {
                Address = user.Address,
                Company = new CompanyResponseDto(user, user.Address.FullAddress)
            });

            if (industryList?.Count > 0)
                query = query.Where(c => industryList.Contains(c.Company.Industry));


            var addresses = await query.GroupBy(x => x.Address.FullAddress)
            .Select(g => new
            {
                Address = g.First().Address,
                Users = g.Select(x => x.Company).ToList()
            })
            .ToListAsync();

            cache.Set(cacheKey, addresses, CacheOptions);
            logger.LogInformation("Фильтрация адресов завершилась успешно");

            return Results.Ok(addresses);
        }
        [Authorize]
        public static async Task<IResult> Change([FromQuery] Guid? id,
            [FromBody] CompanyChangeDto companyDto,
            [FromServices] IDbContextFactory<PriazovContext> factory,
            [FromServices] IOptions<DadataSettings> dadata,
            [FromServices] ILogger<CompanyEndpointsLogger> logger)
        {

            if (id == null)
            {
                logger.LogWarning("Id компании отсутствует");
                return Results.BadRequest("Id пуст");
            }

            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(
                companyDto,
                new ValidationContext(companyDto),
                validationResults,
                validateAllProperties: true
            );

            if (!isValid)
            {
                logger.LogWarning($"Ошибка валидации при создании компании: {validationResults}");
                var errors = validationResults
                    .GroupBy(v => v.MemberNames.FirstOrDefault() ?? "")
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(v => v.ErrorMessage ?? "Неизвестная ошибка").ToArray()
                    );
                return Results.ValidationProblem(errors);
            }

            companyDto.Name = companyDto.Name.Trim();
            companyDto.FullAddress = companyDto.FullAddress.Trim();
            companyDto.Email = companyDto.Email.Trim();
            companyDto.Phone = companyDto.Phone.Trim();
            companyDto.Industry = companyDto.Industry.Trim();
            companyDto.LeaderName = companyDto.LeaderName.Trim();
            companyDto.Description = companyDto.Description?.Trim();

            using var db = await factory.CreateDbContextAsync();

            var company = db.Users.OfType<Company>().Include(c => c.Address).FirstOrDefault(c => c.Id == id);

            if (company == null)
            {
                logger.LogWarning($"Компания не найдена по Id: {id}");
                return Results.NotFound();
            }
                
            if (!_allowedIndustries.Any(i => i == companyDto.Industry))
            {
                logger.LogWarning($"Индустрия не найдена в списке: {companyDto.Industry}");
                return Results.BadRequest("Недопустимое значение индустрии");
            }
                

            if (db.Users.Any(u => u.Email == companyDto.Email &&
            u.Address.FullAddress == companyDto.FullAddress && u.Id != id))
            {
                logger.LogWarning($"Пользователь с таким email и адресом уже существует: {companyDto.Email}, {companyDto.FullAddress}");
                return Results.Conflict("Повтор уникальных данных");
            }
                

            var api = new CleanClientAsync(dadata.Value.ApiKey, dadata.Value.SecretKey);
            var cleanedAddress = await api.Clean<Dadata.Model.Address>(companyDto.FullAddress);

            if (cleanedAddress.result == null)
            {
                logger.LogWarning($"Адрес не найден: {companyDto.FullAddress}");
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
            logger.LogInformation($"Компания успешно изменена: {id}");

            return Results.Ok(new CompanyResponseDto(company, company.Address.FullAddress));
        }
    }
}
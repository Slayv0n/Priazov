using Backend.Models;
using Backend.Models.Dto;
using Backend.Validation;
using DataBase;
using DataBase.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NLog.Config;
using Org.BouncyCastle.Asn1.Ocsp;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Backend.Services
{
    public interface IAuthService
    {
        Task<AuthDto> Login(LoginDto loginDto);
        Task<AuthDto> Refresh(RefreshDto refreshDto);
        Task<RefreshDto> GetRefresh(Guid userId);
        Task<AuthDto?> Logout(RefreshDto refreshDto);
        Task<ClaimsPrincipal> SignInForContext(LoginDto loginDto);
        ClaimsPrincipal CreateClaimsPrincipalFromToken(string accessToken);
    }
    public class AuthService : IAuthService
    {
        private readonly ITokenService _tokenService;
        private readonly IDbContextFactory<PriazovContext> _factory;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthEndpointsLogger> _logger;
        
        public AuthService(ITokenService tokenService,
            IDbContextFactory<PriazovContext> factory,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthEndpointsLogger> logger)
        {
            _tokenService = tokenService;
            _factory = factory;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        public async Task<AuthDto> Login(LoginDto loginDto)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var person = await db.Users
                .Include(u => u.Password)
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);
            if (person == null)
            {
                _logger.LogWarning($"Пользователь не зарегистрирован: {loginDto.Email}");
                throw new UnauthorizedAccessException("Неверный формат учётной записи.");
            }


            if (!PasswordHasher.VerifyPassword(loginDto.Password, person.Password.PasswordHash))
            {
                _logger.LogWarning($"Пользователь не прошёл авторизацию: {loginDto.Email}");
                throw new UnauthorizedAccessException("Неверный формат учётной записи.");
            }

            var newAccessToken = _tokenService.GenerateAccessToken(Convert.ToString(person.Id)!,
                person.Email, person.Role);
            var newRefreshToken = _tokenService.GenerateRefreshToken(Convert.ToString(person.Id)!);

            await db.Sessions.Where(s => s.UserId == person.Id).ExecuteDeleteAsync();

            await db.Sessions.AddAsync(new UserSession()
            {
                RefreshToken = newRefreshToken,
                UserId = person.Id,
                User = person,
                ExpiresAt = DateTime.UtcNow.AddDays(Convert.ToDouble(_jwtSettings.RefreshTokenExpiryDays))
            });

            await db.SaveChangesAsync();
            _logger.LogInformation($"Пользователь успешно авторизовался {loginDto.Email} в {DateTime.UtcNow}");

            return new AuthDto
            {
                Id = person.Id,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                Role = person.Role
            };
        }

        public async Task<AuthDto?> Logout(RefreshDto refreshDto)
        {
            using var db = await _factory.CreateDbContextAsync();

            var principal = _tokenService.ValidateToken(refreshDto.RefreshToken, isAccessToken: false);
            if (principal == null)
            {
                _logger.LogWarning("Токен не валиден");
                throw new UnauthorizedAccessException("Пользователь не авторизован.");
            }

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

            await db.Sessions.Where(s => Convert.ToString(s.UserId) == userId).ExecuteDeleteAsync();
            _logger.LogInformation($"Пользователь успешно вышел {userId} в {DateTime.UtcNow}");

            return null;
        }

        public async Task<AuthDto> Refresh(RefreshDto refreshDto)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var principal = _tokenService.ValidateToken(refreshDto.RefreshToken, isAccessToken: false);
            if (principal == null)
            {
                _logger.LogWarning("Токен не валиден");
                throw new UnauthorizedAccessException("Пользователь не авторизован.");
            }

            var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (userId == Guid.Empty)
            {
                _logger.LogWarning("Пустой или отсутствующий id пользователя");
                throw new UnauthorizedAccessException("Пользователь не авторизован");
            }

            var session = await db.Sessions
                .AsNoTracking()
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (session == null)
            {
                _logger.LogWarning("Сессия не существует");
                throw new UnauthorizedAccessException("Пользователь не авторизован");
            }

            if (session.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Сессия истекла");
                throw new UnauthorizedAccessException("Пользователь не авторизован");
            }

            if (session.RefreshToken != refreshDto.RefreshToken)
            {
                _logger.LogWarning("Токены не совпадают");
                throw new UnauthorizedAccessException("Пользователь не авторизован");
            }

            var newAccessToken = _tokenService.GenerateAccessToken(userId.ToString(), session.User.Email, session.User.Role);
            var newRefreshToken = _tokenService.GenerateRefreshToken(userId.ToString());

            var newUser = new UserSession
            {
                RefreshToken = newRefreshToken,
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)
            };

            await db.Sessions.Where(s => s.UserId == userId)
                .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.RefreshToken, newUser.RefreshToken)
                .SetProperty(s => s.ExpiresAt, newUser.ExpiresAt));

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == newUser.UserId);

            if (user == null)
            {
                _logger.LogWarning("Пользователь не найден");
                throw new NotFoundException("Пользователь не найден");
            }

            _logger.LogInformation($"Сессия продлена {session.Id}");

            return new AuthDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                Id = user.Id,
                Role= user.Role
            };
        }

        public async Task<RefreshDto> GetRefresh(Guid userId)
        {
            using var db = await _factory.CreateDbContextAsync();

            var user = await db.Users.Include(u => u.Session).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.Session == null)
            {
                _logger.LogWarning($"Пользователь не найден: {userId}");
                throw new NotFoundException("Пользователь не найден");
            }

            var refresh = new RefreshDto { RefreshToken = user.Session.RefreshToken };

            return refresh;
        }

        public async Task<ClaimsPrincipal> SignInForContext(LoginDto loginDto)
        {
            using var db = await _factory.CreateDbContextAsync();

            var person = await db.Users
            .Include(u => u.Password)
            .FirstOrDefaultAsync(u => u.Email == loginDto.Email);
            if (person == null)
            {
                _logger.LogWarning($"Пользователь не зарегистрирован: {loginDto.Email}");
                throw new UnauthorizedAccessException("Неверный формат учётной записи.");
            }

            if (!PasswordHasher.VerifyPassword(loginDto.Password, person.Password.PasswordHash))
            {
                _logger.LogWarning($"Пользователь не прошёл авторизацию: {loginDto.Email}");
                throw new UnauthorizedAccessException("Неверный формат учётной записи.");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, person.Id.ToString()),
                new Claim(ClaimTypes.Email, person.Email),
                new Claim(ClaimTypes.Role, person.Role),
                new Claim("exp", ((DateTimeOffset)DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes)).ToUnixTimeSeconds().ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(claimsIdentity);
        }

        public ClaimsPrincipal CreateClaimsPrincipalFromToken(string accessToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(accessToken);

            var identity = new ClaimsIdentity(
                jwtToken.Claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return new ClaimsPrincipal(identity);
        }
    }

}

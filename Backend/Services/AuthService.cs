using Backend.Models;
using Backend.Models.Dto;
using DataBase;
using DataBase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NLog.Config;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Security.Claims;

namespace Backend.Services
{
    public interface IAuthService
    {
        Task<AuthDto> Login(LoginDto loginDto);
        Task<AuthDto> Refresh(RefreshDto refreshDto);
        Task<AuthDto?> Logout(RefreshDto refreshDto);
    }
    public class AuthService : IAuthService
    {
        private readonly ITokenService _tokenService;
        private readonly IDbContextFactory<PriazovContext> _factory;
        private readonly IOptions<JwtSettings> _jwtSettings;
        private readonly ILogger<AuthEndpointsLogger> _logger;
        
        public AuthService(ITokenService tokenService,
            IDbContextFactory<PriazovContext> factory,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthEndpointsLogger> logger)
        {
            _tokenService = tokenService;
            _factory = factory;
            _jwtSettings = jwtSettings;
            _logger = logger;
        }

        public async Task<AuthDto> Login(LoginDto loginDto)
        {
            await using var db = await _factory.CreateDbContextAsync();

            if (string.IsNullOrEmpty(loginDto.Email) || string.IsNullOrEmpty(loginDto.Password))
            {
                _logger.LogWarning($"Отсутствует пароль или почта: {loginDto.Email}, {loginDto.Password}");
                //return Results.BadRequest("Почта и пароль обязательны");
            }


            var person = await db.Users
                .Include(u => u.Password)
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);
            if (person == null)
            {
                _logger.LogWarning($"Пользователь не зарегистрирован: {loginDto.Email}");
                throw new UnauthorizedAccessException("Неверный формат учётной записи");
            }


            if (!PasswordHasher.VerifyPassword(loginDto.Password, person.Password.PasswordHash))
            {
                _logger.LogWarning($"Пользователь не прошёл авторизацию: {loginDto.Email}");
                throw new UnauthorizedAccessException("Неверный формат учётной записи");
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
                ExpiresAt = DateTime.UtcNow.AddDays(Convert.ToDouble(_jwtSettings.Value.RefreshTokenExpiryDays))
            });

            await db.SaveChangesAsync();
            _logger.LogInformation($"Пользователь успешно авторизовался {loginDto.Email} в {DateTime.UtcNow}");

            return new AuthDto
            {
                Id = person.Id,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }

        public async Task<AuthDto?> Logout(RefreshDto refreshDto)
        {
            using var db = await _factory.CreateDbContextAsync();

            var principal = _tokenService.ValidateToken(refreshDto.RefreshToken, isAccessToken: false);
            if (principal == null)
            {
                _logger.LogWarning("Токен не валиден");
                throw new UnauthorizedAccessException("Пользователь не авторизован");
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
                throw new UnauthorizedAccessException("Пользователь не авторизован");
            }

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var session = await db.Sessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == Guid.Parse(userId));

            if (session == null || session.ExpiresAt < DateTime.UtcNow || session.RefreshToken != refreshDto.RefreshToken)
            {
                _logger.LogWarning("Сессия истекла или не существует");
                throw new UnauthorizedAccessException("Пользователь не авторизован");
            }

            var newAccessToken = _tokenService.GenerateAccessToken(userId, session.User.Email, session.User.Role);
            var newRefreshToken = _tokenService.GenerateRefreshToken(userId);

            var newUser = new UserSession
            {
                RefreshToken = newRefreshToken,
                UserId = Guid.Parse(userId),
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.Value.RefreshTokenExpiryDays)
            };

            await db.Sessions.Where(s => s.UserId == Guid.Parse(userId))
                .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.RefreshToken, newUser.RefreshToken)
                .SetProperty(s => s.ExpiresAt, newUser.ExpiresAt));

            _logger.LogInformation($"Сессия продлена {session.Id}");

            return new AuthDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                Id = newUser.Id
            };
        }
    }

}

using Backend.Models;
using DataBase;
using DataBase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Backend.Mapping
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/auth");

            group.MapPost("/login", Login);
            group.MapPost("/refresh", RefreshToken);
            group.MapPost("/logout", Logout);
        }

        private static async Task<IResult> Login(
            LoginRequest request,
            [FromServices] TokenService tokenService,
            [FromServices] IDbContextFactory<PriazovContext> factory,
            [FromServices] IOptions<JwtSettings> jwtSettings,
            [FromServices] ILogger<AuthEndpointsLogger> logger)
        {
            await using var db = await factory.CreateDbContextAsync();

            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                logger.LogWarning($"Отсутствует пароль или почта: {request.Email}, {request.Password}");
                return Results.BadRequest("Почта и пароль обязательны");
            }
                

            var person = await db.Users
                .Include(u => u.Password)
                .FirstOrDefaultAsync(u => u.Email == request.Email);
            if (person == null)
            {
                logger.LogWarning($"Пользователь не зарегистрирован: {request.Email}");
                return Results.Unauthorized();
            }
                

            if (!PasswordHasher.VerifyPassword(request.Password, person.Password.PasswordHash))
            {
                logger.LogWarning($"Пользователь не прошёл авторизацию: {request.Email}");
                return Results.Unauthorized();
            }

            var newAccessToken = tokenService.GenerateAccessToken(Convert.ToString(person.Id)!,
                person.Email, person.Role);
            var newRefreshToken = tokenService.GenerateRefreshToken(Convert.ToString(person.Id)!);

            await db.Sessions.Where(s => s.UserId == person.Id).ExecuteDeleteAsync();

            await db.Sessions.AddAsync(new UserSession()
            {
                RefreshToken = newRefreshToken,
                UserId = person.Id,
                User = person,
                ExpiresAt = DateTime.UtcNow.AddDays(Convert.ToDouble(jwtSettings.Value.RefreshTokenExpiryDays))
            });

            await db.SaveChangesAsync();
            logger.LogInformation($"Пользователь успешно авторизовался {request.Email} в {DateTime.UtcNow}");

            return Results.Ok(new { AccessToken = newAccessToken, RefreshToken = newRefreshToken, person.Email });
        }

        private static async Task<IResult> RefreshToken(
        RefreshRequest request,
        [FromServices] TokenService tokenService,
        [FromServices] IDbContextFactory<PriazovContext> factory,
        [FromServices] IOptions<JwtSettings> jwtSettings,
        [FromServices] ILogger<AuthEndpointsLogger> logger)
        {
            await using var db = await factory.CreateDbContextAsync();

            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                logger.LogWarning("Refresh token отсутствует");
                return Results.BadRequest("Refresh token обязателен");
            }
                

            var principal = tokenService.ValidateToken(request.RefreshToken, isAccessToken: false);
            if (principal == null)
            {
                logger.LogWarning("Токен не валиден");
                return Results.Unauthorized();
            }  

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var session = await db.Sessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == Guid.Parse(userId));

            if (session == null || session.ExpiresAt < DateTime.UtcNow || session.RefreshToken != request.RefreshToken)
            {
                logger.LogWarning("Сессия истекла или не существует");
                return Results.Unauthorized();
            }

            var newAccessToken = tokenService.GenerateAccessToken(userId, session.User.Email, session.User.Role);
            var newRefreshToken = tokenService.GenerateRefreshToken(userId);

            var newUser = new UserSession
            {
                RefreshToken = newRefreshToken,
                UserId = Guid.Parse(userId),
                ExpiresAt = DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpiryDays)
            };

            await db.Sessions.Where(s => s.UserId == Guid.Parse(userId))
                .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.RefreshToken, newUser.RefreshToken)
                .SetProperty(s => s.ExpiresAt, newUser.ExpiresAt));

            logger.LogInformation($"Сессия продлена {session.Id}");

            return Results.Ok(new { AccessToken = newAccessToken, RefreshToken = newRefreshToken });
        }

        private static async Task<IResult> Logout(
            RefreshRequest request,
            [FromServices] TokenService tokenService,
            [FromServices] IDbContextFactory<PriazovContext> factory,
            [FromServices] ILogger<AuthEndpointsLogger> logger)
        {
            await using var db = await factory.CreateDbContextAsync();

            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                logger.LogWarning("Refresh token отсутствует");
                return Results.BadRequest("Refresh token обязателен");
            }

            var principal = tokenService.ValidateToken(request.RefreshToken, isAccessToken: false);
            if (principal == null)
            {
                logger.LogWarning("Токен не валиден");
                return Results.Unauthorized();
            }

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

            await db.Sessions.Where(s => Convert.ToString(s.UserId) == userId).ExecuteDeleteAsync();
            logger.LogInformation($"Пользователь успешно вышел {userId} в {DateTime.UtcNow}");

            return Results.NoContent();
            }
        }
    public record LoginRequest(
    [Required] string Email,
    [Required] string Password
    );
    public record RefreshRequest(
        [Required] string RefreshToken
    );
}

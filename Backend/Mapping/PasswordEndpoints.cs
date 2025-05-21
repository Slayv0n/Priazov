using Backend.Models;
using DataBase;
using DataBase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Mapping
{
    public static class PasswordEndpoints
    {
        public static void MapPasswordEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/password");

            group.MapPost("/forgot-password", ForgotPassword);
            group.MapPost("/reset-password", PostResetPassword);
        }
        private static async Task<IResult> ForgotPassword(ForgotPasswordRequest request,
            [FromServices] EmailService emailService,
            [FromServices] IDbContextFactory<PriazovContext> factory,
            [FromServices] ILogger<PasswordEndpointsLogger> logger)
        {
            await using var db = await factory.CreateDbContextAsync();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                logger.LogWarning("Попытка сброса пароля для несуществующего пользователя");
                return Results.Ok();
            }
                
            var token = new PasswordResetToken
            {
                Token = Guid.NewGuid().ToString()[..6],
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            var update = await db.PasswordResetTokens
                .Where(t => t.UserId == user.Id)
                .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.Token, token.Token)
                .SetProperty(t => t.ExpiresAt, token.ExpiresAt));

            if (update == 0)
            {
                await db.PasswordResetTokens.AddAsync(token);
                await db.SaveChangesAsync();
            }

            await emailService.SendPasswordResetEmail(user.Email, token.Token);

            return Results.Ok();
        }
        private static async Task<IResult> PostResetPassword(ResetPasswordRequest request,
            [FromServices] EmailService emailService,
            [FromServices] IDbContextFactory<PriazovContext> factory,
            [FromServices] ILogger<PasswordEndpointsLogger> logger)
        {
            await using var db = await factory.CreateDbContextAsync();

            var token = await db.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == request.Token && t.ExpiresAt > DateTime.UtcNow);
            if (token == null)
            {
                logger.LogWarning("Недействительный или просроченный токен.");
                return Results.BadRequest("Недействительный или просроченный токен.");
            }      

            var user = await db.Users
                .Include(u => u.Password)
                .Include(u => u.Session)
                .FirstOrDefaultAsync(u => u.Id == token.UserId);
            if (user == null)
            {
                logger.LogWarning($"Пользователь не найден: {token.UserId}");
                return Results.NotFound("Пользователь не найден.");
            }
                
            if (PasswordHasher.VerifyPassword(request.NewPassword, user.Password.PasswordHash))
            {
                return Results.BadRequest("Пароль не должен повторять предыдущий");
            }

            user.Password.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
            user.Password.LastUpdated = DateTime.UtcNow;

            db.PasswordResetTokens.Remove(token);
            if (user.Session != null)
                db.Sessions.Remove(user.Session);

            await db.SaveChangesAsync();

            await emailService.SendPasswordOkayEmail(user.Email);
            logger.LogInformation($"Пароль успешно изменён для пользователя {user.Id}");

            return Results.Ok("Пароль успешно изменён.");
        }
    }
    public record ForgotPasswordRequest(string Email);
    public record ResetPasswordRequest(string Token, string NewPassword);
}

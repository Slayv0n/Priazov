using Backend.Models;
using Backend.Models.Dto;
using DataBase;
using DataBase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NLog.Config;
using System.Linq;

namespace Backend.Services
{
    public interface IPasswordService
    {
        Task ForgotPassword(string addressMessage);
        Task<bool> IsValidToken(string token);
        Task ResetPassword(Guid userId, string newPassword);
    }

    public class PasswordService : IPasswordService
    {
        private readonly IMessageService _messageService;
        private readonly IDbContextFactory<PriazovContext> _factory;
        private readonly ILogger<PasswordEndpointsLogger> _logger;

        public PasswordService(IMessageService messageService,
            IDbContextFactory<PriazovContext> factory,
            ILogger<PasswordEndpointsLogger> logger)
        {
            _messageService = messageService;
            _factory = factory;
            _logger = logger;
        }

        public async Task ForgotPassword(string addressMessage)
        {
            using var db = await _factory.CreateDbContextAsync();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == addressMessage);
            if (user == null)
            {
                _logger.LogWarning("Попытка сброса пароля для несуществующего пользователя");
                throw new Exception("Что-то пошло не так.");
            }

            var token = new PasswordResetToken
            {
                Token = Guid.NewGuid().ToString()[..6],
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddSeconds(90)
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

            await _messageService.SendPasswordResetEmail(user.Email, token.Token);
        }

        public async Task<bool> IsValidToken(string token)
        {
            using var db = await _factory.CreateDbContextAsync();

            var valid = db.PasswordResetTokens.FirstOrDefault(t => t.Token == token);

            if (valid == null || valid.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogWarning("Недействительный или просроченный токен.");
                throw new Exception("Недействительный код.");
            }

            var user = db.Users.FirstOrDefault(u => u.Id == valid.UserId);

            db.PasswordResetTokens.Remove(valid);
            if (user!.Session != null)
                db.Sessions.Remove(user.Session);

            return true;
        }

        public Task ResetPassword(Guid userId, string newPassword)
        {
            throw new NotImplementedException();
        }
    }
}

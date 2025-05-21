using DataBase;
using DataBase.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend
{
    public abstract class BaseCleanupService<TEntity> : BackgroundService
    {
        private readonly IDbContextFactory<PriazovContext> _dbContextFactory;
        private readonly TimeSpan _interval;
        private readonly ILogger<BaseCleanupService<TEntity>> _logger;

        protected BaseCleanupService(IDbContextFactory<PriazovContext> dbContextFactory, TimeSpan interval, ILogger<BaseCleanupService<TEntity>> logger)
        {
            _dbContextFactory = dbContextFactory;
            _interval = interval;
            _logger = logger;
        }

        protected abstract IQueryable<TEntity> GetExpiredQuery(PriazovContext db, DateTime now);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Начало работы background сервиса {typeof(TEntity).Name}");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var db = await _dbContextFactory.CreateDbContextAsync(stoppingToken);
                    var query = GetExpiredQuery(db, DateTime.UtcNow);
                    var deletedCount = await query.ExecuteDeleteAsync(stoppingToken);

                    _logger.LogInformation($"Удалено {deletedCount} {typeof(TEntity).Name} записей");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка в background сервисе: {typeof(TEntity).Name}");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation($"Конец работы background сервиса {typeof(TEntity).Name}");
        }
    }

    public class PasswordTokensCleanupService : BaseCleanupService<PasswordResetToken>
    {
        public PasswordTokensCleanupService(IDbContextFactory<PriazovContext> dbContextFactory, ILogger<PasswordTokensCleanupService> logger)
            : base(dbContextFactory, TimeSpan.FromHours(1), logger) { }

        protected override IQueryable<PasswordResetToken> GetExpiredQuery(PriazovContext db, DateTime now)
            => db.PasswordResetTokens.Where(p => p.ExpiresAt <= now);
    }

    public class SessionsCleanupService : BaseCleanupService<UserSession>
    {
        public SessionsCleanupService(IDbContextFactory<PriazovContext> dbContextFactory, ILogger<SessionsCleanupService> logger)
            : base(dbContextFactory, TimeSpan.FromHours(24), logger) { }

        protected override IQueryable<UserSession> GetExpiredQuery(PriazovContext db, DateTime now)
            => db.Sessions.Where(s => s.ExpiresAt <= now);
    }
}

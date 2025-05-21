using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace DataBase
{
    public interface IDbContextFactory
    {
        PriazovContext CreateDbContext();
    }

    public class DbContextFactory
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionStringName;
        private readonly Action<DbContextOptionsBuilder<PriazovContext>>? _optionsAction;

        public DbContextFactory(
            IConfiguration configuration,
            string connectionStringName,
            Action<DbContextOptionsBuilder<PriazovContext>>? optionsAction = null)
        {
            _configuration = configuration;
            _connectionStringName = connectionStringName;
            _optionsAction = optionsAction;
        }

        public PriazovContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<PriazovContext>();

            // Получаем строку подключения из конфигурации
            var connectionString = _configuration.GetConnectionString(_connectionStringName);

            optionsBuilder.UseNpgsql(connectionString);

            // Применяем дополнительные настройки, если они были переданы
            _optionsAction?.Invoke(optionsBuilder);

            // Создаем экземпляр контекста
            return new PriazovContext(optionsBuilder.Options);
        }
    }
}
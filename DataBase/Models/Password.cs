using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataBase.Models
{
    public class UserPassword
    {
        public Guid Id { get; set; }

        // Внешний ключ для связи с пользователем (1:1)
        [JsonIgnore]
        public Guid UserId { get; set; }
        [JsonIgnore]
        public User User { get; set; } = null!;

        // Хэш пароля
        [MaxLength(256)]
        public string PasswordHash { get; set; } = null!;

        // Дата последнего изменения пароля
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
    public class PasswordResetToken
    {
        public Guid Id { get; set; }
        [MaxLength(6)]
        public string Token { get; set; } = null!; // Уникальный токен
        public Guid UserId { get; set; }   // Связь с пользователем
        public User User { get; set; } = null!;    // Навигационное свойство
        public DateTime ExpiresAt { get; set; }  // Срок действия (UTC)
    }
}

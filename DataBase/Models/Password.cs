using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataBase.Models
{
    public class UserPassword
    {
        public Guid Id { get; set; }
        [JsonIgnore]
        public Guid UserId { get; set; }
        [JsonIgnore]
        public User User { get; set; } = null!;
        // Хэш пароля
        [MaxLength(256)]
        public string PasswordHash { get; set; } = null!;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
    public class PasswordResetToken
    {
        public Guid Id { get; set; }
        [MaxLength(6)]
        public string Token { get; set; } = null!;
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public DateTime ExpiresAt { get; set; } 
    }
}

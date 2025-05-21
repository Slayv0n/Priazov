using JsonProperty.EFCore;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Emit;

namespace DataBase.Models
{
    public class User
    {
        public Guid Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;
        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = null!;
        public UserPassword Password { get; set; } = null!;
        [Required]
        [MaxLength(24)]
        public string Phone { get; set; } = null!;
        public byte[]? PhotoIcon { get; set; }
        [Required]
        [MaxLength(255)]
        public ShortAddressDto Address { get; set; } = null!;
        public UserSession? Session { get; set; }
        public PasswordResetToken? PasswordResetToken { get; set; }
        [MaxLength(12)]
        public string Role { get; set; } = null!;
    }
    public class Company : User
    {
        [MaxLength(100)]
        public string Industry { get; set; } = null!;
        [MaxLength(100)]
        public string LeaderName { get; set; } = null!;
        [MaxLength(1024)]
        public string? Description { get; set; }
        public byte[]? PhotoHeader { get; set; }
        [MaxLength(1024)]
        public JsonList<string> Contacts { get; set; } = new JsonList<string>();
        public List<Project>? Projects { get; set; }
    }
    public class Manager : User;
    public class Admin : User;
}

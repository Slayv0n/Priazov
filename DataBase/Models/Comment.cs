using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DataBase.Models
{
    public class Comment
    {
        public Guid Id { get; set; }
        [StringLength(512)]
        public required string Text { get; set; }
        public Guid UserId { get; set; }
        [JsonIgnore]
        public User User { get; set; } = null!;
        public Guid CompanyId { get; set; }
        [JsonIgnore]
        public Company Company { get; set; } = null!;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}

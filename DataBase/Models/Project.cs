using JsonProperty.EFCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DataBase.Models
{
    public class Project
    {
        public Guid Id { get; set; }
        [MaxLength(150)]
        public string Name { get; set; } = null!;
        [JsonIgnore]
        public Guid CompanyId { get; set; }
        [JsonIgnore]
        public Company Company { get; set; } = null!;
        [MaxLength(1024)]
        public string? Description { get; set; }
        public byte[]? PhotoIcon { get; set; }
        [MaxLength(1024)]
        public JsonList<byte[]> Photos { get; set; } = new JsonList<byte[]>();
    }
}

using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataBase.Models
{
    public class Address
    {
        public Guid Id { get; set; }
        [StringLength(200)]
        public string Street { get; set; } = null!;
        [StringLength(100)]
        public string? Apartment { get; set; }
        [StringLength(50)]
        public string City { get; set; } = null!;
        [StringLength(50)]
        public string Country { get; set; } = "Россия";
        [StringLength(20)]
        public string? PostalCode { get; set; }

        [Column(TypeName = "decimal(10, 7)")]
        public decimal Latitude { get; set; }
        [Column(TypeName = "decimal(10, 7)")]
        public decimal Longitude { get; set; }

        [JsonIgnore]
        public Guid UserId { get; set; }
        [JsonIgnore]
        public User User { get; set; } = null!;

        [NotMapped]
        public string FullAddress => $"{Street}, {City}, {Country}";
    }
    public class ShortAddressDto
    {
        [JsonIgnore]
        public Guid Id { get; set; }
        [JsonIgnore]
        public Guid UserId { get; set; }
        [JsonIgnore]
        public User User { get; set; } = null!;
        [MaxLength(255)]
        public string Region { get; set; } = null!;
        public string FullAddress { get; set; } = null!;
        [Column(TypeName = "decimal(10, 7)")]
        public decimal Latitude { get; set; }
        [Column(TypeName = "decimal(10, 7)")]
        public decimal Longitude { get; set; }
    }
}

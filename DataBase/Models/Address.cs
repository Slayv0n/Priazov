using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataBase.Models
{
    public class Address
    {
        public Guid Id { get; set; }
        [StringLength(200)]
        public string Street { get; set; } = null!; // Улица и дом (пример: "ул. Ленина, 10")
        [StringLength(100)]
        public string? Apartment { get; set; } // Квартира/офис (необязательно)
        [StringLength(50)]
        public string City { get; set; } = null!; // Город
        [StringLength(50)]
        public string Country { get; set; } = "Россия"; // Значение по умолчанию
        [StringLength(20)]
        public string? PostalCode { get; set; } // Почтовый индекс

        // Геокоординаты (обязательно для карт)
        [Column(TypeName = "decimal(10, 7)")]
        public decimal Latitude { get; set; } // Широта
        [Column(TypeName = "decimal(10, 7)")]
        public decimal Longitude { get; set; } // Долгота

        [JsonIgnore]
        public Guid UserId { get; set; }
        [JsonIgnore]
        public User User { get; set; } = null!;

        // Вычисляемое поле для полного адреса (не сохраняется в БД)
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
        public string FullAddress { get; set; } = null!;
        [Column(TypeName = "decimal(10, 7)")]
        public decimal Latitude { get; set; } // Широта
        [Column(TypeName = "decimal(10, 7)")]
        public decimal Longitude { get; set; } // Долгота
    }
}

using System.ComponentModel.DataAnnotations;

namespace DataBase.Models
{
    public class Comment
    {
        public Guid Id { get; set; }
        [StringLength(512)]
        public required string Text { get; set; }
        public Guid UserId { get; set; }
        public Guid CompanyId { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}

using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Dto
{
    public class CommentDto
    {
        [StringLength(512)]
        public required string Text { get; set; }
        public Guid UserId { get; set; }
        public Guid CompanyId { get; set; }
    }
}

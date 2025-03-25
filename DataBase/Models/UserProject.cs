using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataBase.Models
{
    public class UserProject
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public Guid? ProjectId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; } = null!;
        [ForeignKey("ProjectId")]
        public Project? Project { get; set; } = null!;
    }
}

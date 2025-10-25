namespace Backend.Models.Dto
{
    public class ReviewDto
    {
        public int Count { get; set; }
        public List<CompanyResponseDto> Companies { get; set; } = new List<CompanyResponseDto>();
    }
    public class CountDto 
    { 
        public int Count { get; set; }
    } 
}

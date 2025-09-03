using DataBase.Models;

namespace Backend.Models.Dto
{
    public class AddressDto
    {
        public ShortAddressDto Address { get; set; } =  new ShortAddressDto();
        public List<CompanyResponseDto> Companies { get; set; } = new List<CompanyResponseDto>();
        public AddressDto() { }

    }
}

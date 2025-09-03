using Backend.Models.Dto;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Threading.Tasks;

namespace Backend
{
    public class IndexModel : PageModel
    {
        private readonly ICompanyService _companyService;
        public IndexModel(ICompanyService companyService)
        {
            _companyService = companyService;
        }
        public async Task OnGetAsync()
        {
            Query = await _companyService.ReviewCompanyAsync();
            Count = await _companyService.CountCompaniesAsync();
            Count -= Count > 5 ? 5 : Count; 
        }

        public async Task<IActionResult> OnPostUpdateFilterAsync([FromBody] List<Filter>? filters)
        {
            if (!ModelState.IsValid && filters != null)
            {
                return new JsonResult(null);
            }

            Addresses = await _companyService.FilterMapCompanyAsync(filters);
            
            return new JsonResult(Addresses);
        }
        public int Count { get; set; }
        public List<CompanyResponseDto> Query { get; set; } = new List<CompanyResponseDto>();
        public List<AddressDto> Addresses { get; set; } = new List<AddressDto>();
    }
}

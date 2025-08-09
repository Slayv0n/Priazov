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
        private readonly CompanyService _companyService;
        public IndexModel(CompanyService companyService)
        {
            _companyService = companyService;
        }
        public async Task OnGetAsync()
        {
            Query = await _companyService.ReviewCompanyAsync();
            Count = await _companyService.CountCompaniesAsync();
            Count -= Count > 5 ? 5 : Count; 
        }

        public async Task<IActionResult> OnPostUpdateFilterAsync([FromBody] List<Filter> filters)
        {
            if (!ModelState.IsValid)
            {
                return new JsonResult(null);
            }

            StringBuilder sb = new StringBuilder();

            foreach(var select in filters)
            {
                sb.Append(select.Industry);
            }

            Addresses = await _companyService.FilterMapCompanyAsync(sb.ToString());
            
            return new JsonResult(Addresses);
        }
        public int Count { get; set; }
        public List<CompanyResponseDto> Query { get; set; } = new List<CompanyResponseDto>();
        public List<AddressDto> Addresses { get; set; } = new List<AddressDto>();
    }
}

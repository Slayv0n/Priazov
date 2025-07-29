using Backend.Models.Dto;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
            Tuple<List<CompanyResponseDto>, int> tup = await _companyService.ReviewCompanyAsync();
            Query = tup.Item1;
            Count = tup.Item2;
        }
        public int Count { get; set; }
        public List<CompanyResponseDto> Query { get; set; } = new List<CompanyResponseDto>();
    }
}

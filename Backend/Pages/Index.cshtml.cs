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
        private readonly List<Filter> _selectedFilters = new();
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

        public async Task OnPostUpdateFilterAsync([FromBody] Filter filter)
        {
            if (filter.IsChecked)
            {
                _selectedFilters.Add(filter);
            }
            else
            {
                _selectedFilters.Remove(filter);
            }

            StringBuilder sb = new StringBuilder();

            foreach(var select in _selectedFilters)
            {
                sb.Append(select.Industry);
            }

            Adresses = await _companyService.FilterMapCompanyAsync(sb.ToString());
        }

        public int Count { get; set; }
        public List<CompanyResponseDto> Query { get; set; } = new List<CompanyResponseDto>();
        public List<AddressDto> Adresses { get; set; } = new List<AddressDto>();
    }
}

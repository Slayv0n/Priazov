using Backend.Models.Dto;
using Backend.Services;
using DataBase.Models;
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
        private readonly IImageService _imageService;
        public IndexModel(ICompanyService companyService, IImageService imageService)
        {
            _companyService = companyService;
            _imageService = imageService;
        }
        public async Task OnGetAsync()
        {
            Query = await _companyService.ReviewCompanyAsync();
            Count = await _companyService.CountCompaniesAsync();
            Count -= Count > 5 ? 5 : Count;
            foreach (var company in Query)
            {
                bool isAvatar = false;
                var image = await _imageService.Image(company.Id, isAvatar);
                string url;
                if (image.Id == default)
                {
                    url = $"{Request.Scheme}://{Request.Host}/static/img/default/{isAvatar.ToString()}/default.svg";
                }
                else
                {
                    url = $"{Request.Scheme}://{Request.Host}/uploads/users/{company.Id}/{image.FileName}";
                }

                Images.Add(url);
            }
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
        public List<string> Images { get; set; } = new List<string>();
    }
}

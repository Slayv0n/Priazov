using Backend.Models.Dto;
using Backend.Services;
using Backend.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Net.Mime.MediaTypeNames;

namespace Backend.Pages
{
    public class CatalogModel : PageModel
    {
        private readonly ICompanyService _companyService;
        private readonly IImageService _imageService;
        [BindProperty]
        public RequestModel CatalogRequest { get; set; }
        public CatalogModel(ICompanyService companyService, IImageService imageService)
        {
            _companyService = companyService;
            _imageService = imageService;
        }
        public async Task OnGetAsync()
        {
            Query = await _companyService.SearchCompanyAsync(null, null, null);
            foreach (var company in Query)
            {
                bool isAvatar = false;
                var image = await _imageService.Image(company.Id, isAvatar);
                string url;
                if (image.Id == default)
                {
                    url = $"{Request.Scheme}://{Request.Host}/static/img/default/{isAvatar.ToString()}/default.png";
                }
                else
                {
                    url = $"{Request.Scheme}://{Request.Host}/uploads/users/{company.Id}/{image.FileName}";
                }

                Images.Add(url);
            }
        }
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            Query = await _companyService.SearchCompanyAsync(
            CatalogRequest.SearchTerm,
            CatalogRequest.Industry,
            CatalogRequest.Region
            );

            Images.Clear();
            foreach (var company in Query)
            {
                bool isAvatar = false;
                var image = await _imageService.Image(company.Id, isAvatar);
                string url;
                if (image.Id == default)
                {
                    url = $"{Request.Scheme}://{Request.Host}/static/img/default/{isAvatar.ToString()}/default.png";
                }
                else
                {
                    url = $"{Request.Scheme}://{Request.Host}/uploads/users/{company.Id}/{image.FileName}";
                }
                Images.Add(url);
            }

            return Page();
        }
        public class RequestModel 
        {
            public string? SearchTerm { get; set; }
            public string? Industry { get; set; }
            public string? Region { get; set; }
        }

        public List<CompanyResponseDto> Query { get; set; } = new List<CompanyResponseDto>();
        public List<string> Images { get; set; } = new List<string>();
    }
}

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
        public RequestModel CatalogRequest { get; set; } = null!;
        public CatalogModel(ICompanyService companyService, IImageService imageService)
        {
            _companyService = companyService;
            _imageService = imageService;
            CountPages = new();
        }
        public async Task<IActionResult> OnGetAsync([FromRoute] int pageId = 1)
        {
            PageId = pageId;
            try
            {
                Query = await _companyService.SearchCompanyAsync(null, null, null, pageId, CountPages);
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
            catch(Exception ex)
            {
                return RedirectToPage("Error", new { errorCode = Response.StatusCode, errorMessage = ex.Message });
            }
        }
        public async Task<IActionResult> OnPostAsync([FromRoute] int pageId = 1)
        {
            PageId = pageId;
            if (!ModelState.IsValid)
            {
                return Page();
            }
            try
            {
                Query = await _companyService.SearchCompanyAsync(
                    CatalogRequest.SearchTerm,
                    CatalogRequest.Industry,
                    CatalogRequest.Region,
                    pageId,
                    CountPages);

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
            catch (NotFoundException ex)
            {
                return RedirectToPage("Error", new { errorCode = "404", errorMessage = ex.Message });
            }
            catch (Exception ex)
            {
                return RedirectToPage("Error", new {errorMessage = ex.Message });
            }

        }
        public class RequestModel 
        {
            public string? SearchTerm { get; set; }
            public string? Industry { get; set; }
            public string? Region { get; set; }
        }

        public List<CompanyResponseDto> Query { get; set; } = new List<CompanyResponseDto>();
        public List<string> Images { get; set; } = new List<string>();
        public CountDto CountPages;
        public int PageId;
    }
}

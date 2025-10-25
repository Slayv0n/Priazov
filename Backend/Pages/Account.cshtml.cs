using Backend.Models.Dto;
using Backend.Services;
using Backend.Validation;
using DataBase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;

namespace Backend.Pages
{
    public class AccountModel : PageModel
    {
        private readonly ICompanyService _companyService;
        private readonly IImageService _imageService;
        public AccountModel(ICompanyService companyService, IImageService imageService)
        {
            _companyService = companyService;
            _imageService = imageService;
        }
        public async Task<IActionResult> OnGetAsync([FromRoute] Guid userId)
        {
            try
            {
                Company = await _companyService.AccountCompanyAsync(userId);
                var image = await _imageService.Image(userId, true);
                if (image.Id == default)
                {
                    AvatarUrl = $"{Request.Scheme}://{Request.Host}/static/img/default/True/default.png";
                }
                else
                {
                    AvatarUrl = $"{Request.Scheme}://{Request.Host}/uploads/users/{userId}/{image.FileName}";
                }
                image = await _imageService.Image(userId, false);
                if (image.Id == default)
                {
                    MainUrl = $"{Request.Scheme}://{Request.Host}/static/img/profile/дефолт фон профиля.png";
                }
                else
                {
                    MainUrl = $"{Request.Scheme}://{Request.Host}/uploads/users/{userId}/{image.FileName}";
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
        public CompanyResponseDto Company = new();
        public string AvatarUrl = string.Empty;
        public string MainUrl = string.Empty;
    }
}

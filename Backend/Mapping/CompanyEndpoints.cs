using Backend.Models;
using Backend.Models.Dto;
using Backend.Services;
using Backend.Validation;
using Dadata;
using Dadata.Model;
using DataBase;
using DataBase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Org.BouncyCastle.Asn1.Ocsp;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Backend.Mapping
{
    public static class CompanyEndpoints
    {



        public static void MapCompanyEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/companies");
            //group.MapPost("/create", Create);
            //group.MapGet("/review", Review);
            group.MapGet("account", Account);
            //group.MapGet("/filterMap", FilterMap);
            group.MapGet("/search", Search);
            group.MapPut("/update", Update);
        }

        //private static async Task<IResult> Create(
        //    [FromBody] CompanyCreateDto companyDto,
        //    [FromServices] CompanyService service)
        //{
        //    return await service.CreateCompanyAsync(companyDto);
        //}

        //private static async Task<IResult> Review(
        //    [FromServices] CompanyService service)
        //{
        //    //return await service.ReviewCompanyAsync();
        //}

        [Authorize]
        public static async Task<IResult> Account([FromQuery] Guid? id,
            [FromServices] CompanyService service)
        {
            return await service.AccountCompanyAsync(id);
        }

        [Authorize]
        private static async Task<IResult> Search(
            [FromQuery] string? industry,
            [FromQuery] string? region,
            [FromQuery] string? searchTerm,
            [FromServices] CompanyService service)
        {
            return await service.SearchCompanyAsync(industry, region, searchTerm);
        }
        //public static async Task<IResult> FilterMap(
        //    [FromQuery] string? industries,
        //    [FromServices] CompanyService service)
        //{
        //    return await service.FilterMapCompanyAsync(industries);
        //}
        //[Authorize]
        public static async Task<IResult> Update([FromQuery] Guid? id,
            [FromBody] CompanyChangeDto companyDto,
            [FromServices] CompanyService service)
        {
            return await service.UpdateCompanyAsync(id, companyDto);
        }
    }
}
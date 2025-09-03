using Backend.Models.Dto;
using DataBase.Models;
using DataBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Dadata;
using Microsoft.Extensions.Options;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Backend.Services;

namespace Backend.Mapping
{
    public static class ManagerEndpoints
    {


        public static void MapManagerEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/managers");
            group.MapPost("/create", Create);
            group.MapGet("account", Account);
            group.MapPut("/update", Update);
        }

        private static async Task<IResult> Create(
            [FromBody] ManagerCreateDto managerDto,
            [FromServices] ManagerService service)
        {
            return await service.CreateManagerAsync(managerDto);
        }

        [Authorize]
        public static async Task<IResult> Account([FromQuery] Guid? id,
            [FromServices] ManagerService service)
        {
            return await service.AccountManagerAsync(id);
        }

        public static async Task<IResult> Update([FromQuery] Guid? id,
            [FromBody] ManagerChangeDto managerDto,
            [FromServices] ManagerService service)
        {
            return await service.UpdateManagerAsync(id, managerDto);
        }
    }
}

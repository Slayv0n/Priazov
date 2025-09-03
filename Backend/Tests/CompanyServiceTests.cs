using Xunit;
using Moq;
using Backend.Services;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using DataBase;
using DataBase.Models;
using Backend.Models.Dto;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Dadata;
using Backend.Models;
using JsonProperty.EFCore;
using Microsoft.AspNetCore.Http.HttpResults;

public class CompanyServiceTests
{
    [Fact]
    public async Task UpdateCompanyAsync_ValidData_ReturnsOk()
    {
        // Arrange
        var mockFactory = new Mock<IDbContextFactory<PriazovContext>>();
        var mockOptions = new Mock<IOptions<DadataSettings>>();
        var mockLogger = new Mock<ILogger<ManagerService>>();
        var mockCache = new Mock<IMemoryCache>();

        var dadataSettings = new DadataSettings { ApiKey = "key", SecretKey = "secret" };
        mockOptions.Setup(o => o.Value).Returns(dadataSettings);

        var optionsBuilder = new DbContextOptionsBuilder<PriazovContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString());

        var context = new PriazovContext(optionsBuilder.Options);

        // Добавим в БД компанию для обновления
        var existingCompany = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Old Name",
            Email = "old@example.com",
            Phone = "123",
            Industry = "Коммерческая деятельность",
            Address = new ShortAddressDto { FullAddress = "Old address" },
            Contacts = new JsonList<string>(),
            LeaderName = "Leader",
            Description = "Desc"
        };

        context.Users.Add(existingCompany);
        await context.SaveChangesAsync();

        mockFactory.Setup(f => f.CreateDbContextAsync(default))
                   .ReturnsAsync(context);

        var service = new CompanyService(
            mockFactory.Object,
            mockOptions.Object,
            email: null,          // не используем в тесте
            turnstile: null,      // не используем
            mockLogger.Object,
            mockCache.Object
        );

        var dto = new CompanyChangeDto
        {
            Name = "New Name",
            Email = "new@example.com",
            Phone = "456",
            Industry = "Коммерческая деятельность",
            FullAddress = "New address",
            Contacts = new JsonList<string>() { VirtualList = new List<string>() { "contact"} },
            LeaderName = "New Leader",
            Description = "New Description",
            PhotoIcon = null,
            PhotoHeader = null
        };

        // Act
        var result = await service.UpdateCompanyAsync(existingCompany.Id, dto);

        // Assert
        var okResult = Assert.IsType<Ok<CompanyResponseDto>>(result);
        Assert.Equal("New Name", okResult.Value.Name);
        Assert.Equal("new@example.com", okResult.Value.Email);
        Assert.Equal("New Leader", okResult.Value.LeaderName);
    }
}

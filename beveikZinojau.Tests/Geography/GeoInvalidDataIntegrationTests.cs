
using System.Net;
using System.Net.Http.Json;
using beveikZinojau.lt.Data;
using beveikZinojau.lt.Models;
using beveikZinojau.lt.Services.Geography;
using beveikZinojau.lt.Services.Geography.GeoGameRecords;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace beveikZinojau.Tests.Geography;

public class GeoInvalidDataIntegrationTests
{
    [Fact]
    public async Task GetQuestion_NoCountries_ReturnsServiceUnavailable()
    {
        var repositoryMock = new Mock<ICountryRepository>();

        repositoryMock
            .Setup(r => r.GetCountriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Country>)Array.Empty<Country>());

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ICountryRepository>();
                    services.AddSingleton<ICountryRepository>(
                        repositoryMock.Object);

                    services.RemoveAll<
                        IDbContextOptionsConfiguration<AppDbContext>>();
                    services.RemoveAll<DbContextOptions<AppDbContext>>();

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(
                            "Host=127.0.0.1;Port=1;Database=unused_test_db;Username=test;Password=test"));
                });
            });

        using var client = factory.CreateClient();

        var sessionResponse = await client.PostAsJsonAsync(
            "/api/geoquestions/session",
            new
            {
                questionCount = 2,
                questionTypes = new[] { "capital" }
            });

        sessionResponse.EnsureSuccessStatusCode();

        var session = await sessionResponse.Content
            .ReadFromJsonAsync<GeoSessionCreated>();

        Assert.NotNull(session);

        var response = await client.PostAsJsonAsync(
            $"/api/geoquestions/session/{session.SessionId}/questions",
            new { });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        repositoryMock.Verify(
            r => r.GetCountriesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using beveikZinojau.lt.Services.Geography.GeoGameRecords;

namespace beveikZinojau.Tests.Geography;

public class GeoQuestionsIntegrationTests
{
    [Fact]
    public async Task StartSession_ValidRequest_ReturnsSession()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:DefaultConnection"] =
                                "Host=127.0.0.1;Port=1;Database=unused_test_db;Username=test;Password=test"
                        });
                });
            });

        using var client = factory.CreateClient();

        var request = new
        {
            questionCount = 3,
            questionTypes = new[] { "capital" }
        };

        using var response = await client.PostAsJsonAsync(
            "/api/geoquestions/session",
            request
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var session = await response.Content
            .ReadFromJsonAsync<GeoSessionCreated>();

        Assert.NotNull(session);
        Assert.Equal(3, session.QuestionCount);
        Assert.Contains("capital", session.QuestionTypes);
        Assert.NotEqual(Guid.Empty, session.SessionId);
        Assert.NotEqual(Guid.Empty, session.PlayerId);
    }
}
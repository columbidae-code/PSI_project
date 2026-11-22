
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

public class GeoGameFlowIntegrationTests
{
    [Fact]
    public async Task FullGame_TwoSkippedQuestions_ReturnsCorrectResult()
    {
        var countries = new List<Country>
        {
            new() { Iso2 = "LT", Name = "Lietuva", Capital = "Vilnius" },
            new() { Iso2 = "LV", Name = "Latvija", Capital = "Ryga" },
            new() { Iso2 = "EE", Name = "Estija", Capital = "Talinas" },
            new() { Iso2 = "PL", Name = "Lenkija", Capital = "Varšuva" },
            new() { Iso2 = "DE", Name = "Vokietija", Capital = "Berlynas" },
            new() { Iso2 = "FR", Name = "Prancūzija", Capital = "Paryžius" },
            new() { Iso2 = "ES", Name = "Ispanija", Capital = "Madridas" },
            new() { Iso2 = "IT", Name = "Italija", Capital = "Roma" }
        };

        var repositoryMock = new Mock<ICountryRepository>();

        repositoryMock
            .Setup(repo => repo.GetCountriesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Country>)countries);

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ICountryRepository>();

                    services.AddSingleton<ICountryRepository>(
                        repositoryMock.Object
                    );

                    services.RemoveAll<
                        IDbContextOptionsConfiguration<AppDbContext>>();

                    services.RemoveAll<DbContextOptions<AppDbContext>>();

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(
                            "Host=127.0.0.1;Port=1;Database=unused_test_db;Username=test;Password=test"
                        ));
                });
            });

        using var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        Assert.Equal(
            "unused_test_db",
            db.Database.GetDbConnection().Database
        );

        var sessionResponse = await client.PostAsJsonAsync(
            "/api/geoquestions/session",
            new
            {
                questionCount = 2,
                questionTypes = new[] { "capital" }
            }
        );

        Assert.Equal(HttpStatusCode.OK, sessionResponse.StatusCode);

        var session = await sessionResponse.Content
            .ReadFromJsonAsync<GeoSessionCreated>();

        Assert.NotNull(session);

        string url = $"/api/geoquestions/session/{session.SessionId}";

        for (int i = 1; i <= 2; i++)
        {
            var questionResponse = await client.PostAsJsonAsync(
                $"{url}/questions",
                new { }
            );

            Assert.Equal(
                HttpStatusCode.OK,
                questionResponse.StatusCode
            );

            var question = await questionResponse.Content
                .ReadFromJsonAsync<GeoQuestionIssued>();

            Assert.NotNull(question);
            Assert.Equal(i, question.QuestionNumber);

            var answerResponse = await client.PostAsJsonAsync(
                $"{url}/answer",
                new
                {
                    questionId = question.QuestionId,
                    playerId = session.PlayerId,
                    selectedOptionIndex = (int?)null
                }
            );

            Assert.Equal(
                HttpStatusCode.OK,
                answerResponse.StatusCode
            );

            var answer = await answerResponse.Content
                .ReadFromJsonAsync<GeoAnswerChecked>();

            Assert.NotNull(answer);
            Assert.False(answer.IsCorrect);
            Assert.Equal(0, answer.PointsEarned);
        }

        var resultResponse = await client.GetAsync(
            $"{url}/result?playerId={session.PlayerId}"
        );

        Assert.Equal(
            HttpStatusCode.OK,
            resultResponse.StatusCode
        );

        var result = await resultResponse.Content
            .ReadFromJsonAsync<GeoGameResult>();

        Assert.NotNull(result);

        Assert.Equal(2, result.TotalQuestions);
        Assert.Equal(0, result.CorrectAnswers);
        Assert.Equal(2, result.WrongAnswers);
        Assert.Equal(0, result.TotalPoints);

        repositoryMock.Verify(
            repo => repo.GetCountriesAsync(
                It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );
    }
}

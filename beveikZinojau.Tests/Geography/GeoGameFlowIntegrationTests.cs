using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using beveikZinojau.lt.Services.Geography.GeoGameRecords;

namespace beveikZinojau.Tests.Geography;

public class GeoGameFlowIntegrationTests
{
    [Fact]
    public async Task FullGame_TwoSkippedQuestions_ReturnsCorrectResult()
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

            Assert.Equal(HttpStatusCode.OK, questionResponse.StatusCode);

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

            Assert.Equal(HttpStatusCode.OK, answerResponse.StatusCode);

            var answer = await answerResponse.Content
                .ReadFromJsonAsync<GeoAnswerChecked>();

            Assert.NotNull(answer);
            Assert.False(answer.IsCorrect);
            Assert.Equal(0, answer.PointsEarned);
        }

        var resultResponse = await client.GetAsync(
            $"{url}/result?playerId={session.PlayerId}"
        );

        Assert.Equal(HttpStatusCode.OK, resultResponse.StatusCode);

        var result = await resultResponse.Content
            .ReadFromJsonAsync<GeoGameResult>();

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalQuestions);
        Assert.Equal(0, result.CorrectAnswers);
        Assert.Equal(2, result.WrongAnswers);
        Assert.Equal(0, result.TotalPoints);
    }
}
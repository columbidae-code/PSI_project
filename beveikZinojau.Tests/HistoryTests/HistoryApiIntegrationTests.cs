using System.Net;
using System.Net.Http.Json;
using beveikZinojau.lt.Data;
using beveikZinojau.lt.Models;
using beveikZinojau.lt.Services.History;
using beveikZinojau.lt.Services.History.HistoryGameRecords;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace beveikZinojau.Tests.HistoryTests;

public sealed class HistoryApiIntegrationTests
{
    [Fact]
    public async Task FullGame_WrongAnswer_ReturnsCompletedResult()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        using var factory = await CreateFactory(connection);
        using var client = factory.CreateClient();

        var sessionResponse = await client.PostAsJsonAsync(
            "/api/historyquestions/session",
            new
            {
                questionCount = 1,
                scope = "All",
                period = "All",
                difficulty = DifficultyLevel.Easy
            });

        Assert.Equal(HttpStatusCode.OK, sessionResponse.StatusCode);

        var session = await sessionResponse.Content.ReadFromJsonAsync<HistorySessionCreated>();
        Assert.NotNull(session);

        string sessionUrl = $"/api/historyquestions/session/{session.SessionId}";
        var questionResponse = await client.PostAsync($"{sessionUrl}/questions", content: null);

        Assert.Equal(HttpStatusCode.OK, questionResponse.StatusCode);

        var question = await questionResponse.Content.ReadFromJsonAsync<HistoryQuestionIssued>();
        Assert.NotNull(question);
        Assert.Equal(1, question.QuestionNumber);
        Assert.Equal(1, question.TotalQuestions);
        Assert.Equal(4, question.Cards.Count);

        var answerResponse = await client.PostAsJsonAsync(
            $"{sessionUrl}/answer",
            new
            {
                playerId = session.PlayerId,
                questionId = question.QuestionId,
                orderedEventIds = question.Cards
                    .Select(card => card.Id)
                    .Reverse()
                    .ToArray()
            });

        Assert.Equal(HttpStatusCode.OK, answerResponse.StatusCode);

        var answer = await answerResponse.Content.ReadFromJsonAsync<HistoryAnswerChecked>();
        Assert.NotNull(answer);
        Assert.False(answer.IsCorrect);
        Assert.Equal(0, answer.PointsEarned);
        Assert.Equal(4, answer.CorrectTimeline.Count);

        var resultResponse = await client.GetAsync(
            $"{sessionUrl}/result?playerId={session.PlayerId}");

        Assert.Equal(HttpStatusCode.OK, resultResponse.StatusCode);

        var result = await resultResponse.Content.ReadFromJsonAsync<HistoryGameResult>();
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalQuestions);
        Assert.Equal(0, result.CorrectAnswers);
        Assert.Equal(1, result.WrongAnswers);
        Assert.Equal(0, result.TotalPoints);
    }

    [Fact]
    public async Task InvalidSubmission_ReturnsBadRequest_AndUnknownSessionReturnsNotFound()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        using var factory = await CreateFactory(connection);
        using var client = factory.CreateClient();

        var invalidSessionResponse = await client.PostAsJsonAsync(
            "/api/historyquestions/session",
            new
            {
                questionCount = 0,
                scope = "All",
                period = "All",
                difficulty = DifficultyLevel.Easy
            });

        Assert.Equal(HttpStatusCode.BadRequest, invalidSessionResponse.StatusCode);

        var missingQuestionResponse = await client.PostAsync(
            $"/api/historyquestions/session/{Guid.NewGuid()}/questions",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, missingQuestionResponse.StatusCode);

        var sessionResponse = await client.PostAsJsonAsync(
            "/api/historyquestions/session",
            new
            {
                questionCount = 1,
                scope = "All",
                period = "All",
                difficulty = DifficultyLevel.Easy
            });

        Assert.Equal(HttpStatusCode.OK, sessionResponse.StatusCode);

        var session = await sessionResponse.Content.ReadFromJsonAsync<HistorySessionCreated>();
        Assert.NotNull(session);

        string sessionUrl = $"/api/historyquestions/session/{session.SessionId}";
        var questionResponse = await client.PostAsync($"{sessionUrl}/questions", content: null);
        Assert.Equal(HttpStatusCode.OK, questionResponse.StatusCode);

        var question = await questionResponse.Content.ReadFromJsonAsync<HistoryQuestionIssued>();
        Assert.NotNull(question);

        var answerResponse = await client.PostAsJsonAsync(
            $"{sessionUrl}/answer",
            new
            {
                playerId = session.PlayerId,
                questionId = question.QuestionId,
                orderedEventIds = Array.Empty<string>()
            });

        Assert.Equal(HttpStatusCode.BadRequest, answerResponse.StatusCode);
    }

    private static async Task<WebApplicationFactory<Program>> CreateFactory(
        SqliteConnection connection)
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<
                        IDbContextOptionsConfiguration<AppDbContext>>();
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlite(connection));
                });
            });

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        db.HistoryEvents.AddRange(TestEvents);
        await db.SaveChangesAsync();

        return factory;
    }

    private static readonly IReadOnlyList<HistoryEvent> TestEvents =
    [
        new HistoryEvent { Id = "event-1", Scope = "World", Period = "Senovė", Title = "First", DisplayDate = "100 BC", Year = -100 },
        new HistoryEvent { Id = "event-2", Scope = "World", Period = "Viduramžiai", Title = "Second", DisplayDate = "1200", Year = 1200 },
        new HistoryEvent { Id = "event-3", Scope = "World", Period = "Naujieji amžiai", Title = "Third", DisplayDate = "1700", Year = 1700 },
        new HistoryEvent { Id = "event-4", Scope = "World", Period = "Modernybė", Title = "Fourth", DisplayDate = "2020", Year = 2020 }
    ];
}

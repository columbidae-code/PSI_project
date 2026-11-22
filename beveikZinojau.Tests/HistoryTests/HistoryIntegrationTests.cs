using beveikZinojau.lt.Data;
using beveikZinojau.lt.Models;
using beveikZinojau.lt.Scoring;
using beveikZinojau.lt.Services.History;
using beveikZinojau.lt.Services.History.HistoryGameEnum;
using beveikZinojau.lt.Services.History.HistoryGameRecords;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using Xunit;

namespace beveikZinojau.Tests.HistoryTests;

public sealed class HistoryIntegrationTests
{
    [Fact]
    public async Task ControllerFlow_ReturnsQuestionAndChecksAnswer()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var contextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new AppDbContext(contextOptions);
        await context.Database.EnsureCreatedAsync();
        context.Users.Add(new AppUser
        {
            Id = "history-user",
            UserName = "history-user",
            NormalizedUserName = "HISTORY-USER"
        });
        await context.SaveChangesAsync();

        var service = CreateService(cache, context);
        var controller = new beveikZinojau.lt.Controllers.HistoryQuestionsController(cache, service);
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "history-user")],
                    "Test"))
            }
        };

        var start = controller.StartSession(
            new HistorySessionCreateRequest(1, "All", "All", DifficultyLevel.Easy));
        var session = Assert.IsType<OkObjectResult>(start).Value as HistorySessionCreated;
        Assert.NotNull(session);

        var questionResult = controller.GetQuestion(session.SessionId);
        var question = Assert.IsType<OkObjectResult>(questionResult).Value as HistoryQuestionIssued;
        Assert.NotNull(question);

        var answerResult = controller.CheckAnswer(
            session.SessionId,
            new HistoryAnswerRequest(
                session.PlayerId,
                question.QuestionId,
                ChronologicalIds));

        var answer = Assert.IsType<OkObjectResult>(answerResult).Value as HistoryAnswerChecked;
        Assert.NotNull(answer);
        Assert.True(answer.IsCorrect);

        var resultAction = await controller.GetResult(session.SessionId, session.PlayerId);
        var result = Assert.IsType<OkObjectResult>(resultAction).Value as HistoryGameResult;
        Assert.NotNull(result);
        Assert.Equal(100, result.AccuracyPercent);

        Assert.Single(await context.Categories.ToListAsync());
        Assert.Single(await context.GameResults.ToListAsync());
        Assert.Single(await context.UserStats.ToListAsync());
    }

    [Fact]
    public void InvalidAnswerAndUnknownSessionUseDefinedErrors()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(cache, null);

        var (created, _) = service.CreateSession(
            new HistorySessionCreateRequest(1, "All", "All", DifficultyLevel.Easy),
            "user-1");
        Assert.NotNull(created);

        var (question, _) = service.GetQuestion(created.SessionId);
        Assert.NotNull(question);

        var (answer, error) = service.CheckAnswer(
            created.SessionId,
            new HistoryAnswerRequest(created.PlayerId, question.QuestionId, Array.Empty<string>()));

        Assert.Null(answer);
        Assert.Equal(HistoryGameError.InvalidSubmission, error);

        var (_, missingError) = service.CheckAnswer(
            Guid.NewGuid(),
            new HistoryAnswerRequest(created.PlayerId, question.QuestionId, ChronologicalIds));
        Assert.Equal(HistoryGameError.SessionNotFound, missingError);
    }

    private static HistoryGameService CreateService(IMemoryCache cache, AppDbContext? context)
    {
        var generator = new HistoryQuestionGenerator(
            new IntegrationHistoryRepository(Events));
        var scoring = new ScoringCalculator(new ScoringConfig
        {
            BasePoints = 100,
            CorrectAnswerShare = 0.5,
            EasyMultiplier = 1,
            MediumMultiplier = 1.5,
            HardMultiplier = 2
        });
        var statsTracker = context is null ? null : new StatsTracker(context);

        return new HistoryGameService(cache, generator, scoring, statsTracker!);
    }

    private static readonly IReadOnlyList<HistoryEvent> Events =
    [
        new HistoryEvent { Id = "i-1", Scope = "World", Period = "Senovė", Title = "Oldest", DisplayDate = "100 BC", Year = -100 },
        new HistoryEvent { Id = "i-2", Scope = "World", Period = "Viduramžiai", Title = "Middle", DisplayDate = "1200", Year = 1200 },
        new HistoryEvent { Id = "i-3", Scope = "World", Period = "Naujieji amžiai", Title = "Newer", DisplayDate = "1700", Year = 1700 },
        new HistoryEvent { Id = "i-4", Scope = "World", Period = "Modernybė", Title = "Newest", DisplayDate = "2020", Year = 2020 }
    ];

    private static readonly IReadOnlyList<string> ChronologicalIds =
        Events.OrderBy(e => e.Year).ThenBy(e => e.ExactDate ?? string.Empty).Select(e => e.Id).Take(4).ToList();

    private sealed class IntegrationHistoryRepository(IReadOnlyList<HistoryEvent> events) : IHistoryRepository
    {
        public IReadOnlyList<HistoryEvent> GetEvents() => events;
    }
}

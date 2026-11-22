using beveikZinojau.lt.Models;
using beveikZinojau.lt.Scoring;
using beveikZinojau.lt.Services.History;
using beveikZinojau.lt.Services.History.HistoryGameRecords;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace beveikZinojau.Tests.HistoryTests;

public sealed class HistoryUnitTests
{
    [Fact]
    public void CreateSession_NormalizesScopeAndPeriod()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(cache);

        var (session, error) = service.CreateSession(
            new HistorySessionCreateRequest(
                2,
                "  Pasaulis  ",
                "  Viduramžiai  ",
                DifficultyLevel.Medium));

        Assert.Null(error);
        Assert.NotNull(session);
        Assert.Equal("Pasaulis", session.Scope);
        Assert.Equal("Viduramžiai", session.Period);
        Assert.Equal(2, session.QuestionCount);
        Assert.NotEqual(Guid.Empty, session.PlayerId);
    }

    [Fact]
    public void CreateSession_RejectsNonPositiveQuestionCount()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(cache);

        var (session, error) = service.CreateSession(
            new HistorySessionCreateRequest(0, "All", "All", DifficultyLevel.Easy));

        Assert.Null(session);
        Assert.Equal("Question count must be positive.", error);
    }

    [Fact]
    public void GetQuestion_ReturnsMissingSession()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(cache);

        var (question, missing) = service.GetQuestion(Guid.NewGuid());

        Assert.Null(question);
        Assert.True(missing);
    }

    [Fact]
    public void CheckAnswer_IsCorrectAndDuplicateAnswerIsRejected()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(cache);

        var (created, _) = service.CreateSession(
            new HistorySessionCreateRequest(1, "All", "All", DifficultyLevel.Easy),
            "user-1");
        Assert.NotNull(created);

        var (question, missing) = service.GetQuestion(created.SessionId);
        Assert.False(missing);
        Assert.NotNull(question);

        var chronologicalIds = StubHistoryEvents
            .OrderBy(e => e.Year)
            .ThenBy(e => e.ExactDate ?? string.Empty)
            .Take(4)
            .Select(e => e.Id)
            .ToList();

        var (answer, error) = service.CheckAnswer(
            created.SessionId,
            new HistoryAnswerRequest(created.PlayerId, question.QuestionId, chronologicalIds));

        Assert.Equal(beveikZinojau.lt.Services.History.HistoryGameEnum.HistoryGameError.None, error);
        Assert.NotNull(answer);
        Assert.True(answer.IsCorrect);
        Assert.Equal(4, answer.CorrectTimeline.Count);
        Assert.True(answer.PointsEarned > 0);

        var (duplicate, duplicateError) = service.CheckAnswer(
            created.SessionId,
            new HistoryAnswerRequest(created.PlayerId, question.QuestionId, chronologicalIds));

        Assert.Null(duplicate);
        Assert.Equal(beveikZinojau.lt.Services.History.HistoryGameEnum.HistoryGameError.AlreadyAnswered, duplicateError);
    }

    [Fact]
    public void Generator_FiltersAndDeduplicatesEvents()
    {
        var generator = new HistoryQuestionGenerator(
            new StubHistoryRepository(StubHistoryEvents));

        var timeline = generator.GenerateTimeline("World", "Senovė", DifficultyLevel.Easy);

        Assert.NotNull(timeline);
        Assert.Equal(4, timeline.ShuffledCards.Count);
        Assert.Equal(4, timeline.CorrectChronologicalEvents.Count);
        Assert.Equal(
            timeline.CorrectChronologicalEvents.Select(e => e.Id),
            timeline.CorrectChronologicalEvents
                .OrderBy(e => e.Year)
                .ThenBy(e => e.ExactDate ?? string.Empty)
                .Select(e => e.Id));
    }

    private static HistoryGameService CreateService(IMemoryCache cache)
    {
        var generator = new HistoryQuestionGenerator(
            new StubHistoryRepository(StubHistoryEvents));
        var scoring = new ScoringCalculator(new ScoringConfig
        {
            BasePoints = 100,
            CorrectAnswerShare = 0.5,
            EasyMultiplier = 1,
            MediumMultiplier = 1.5,
            HardMultiplier = 2
        });

        return new HistoryGameService(cache, generator, scoring, null!);
    }

    private static readonly IReadOnlyList<HistoryEvent> StubHistoryEvents =
    [
        new HistoryEvent { Id = "world-1", Scope = "World", Period = "Senovė", Title = "One", DisplayDate = "100 BC", Year = -100 },
        new HistoryEvent { Id = "world-2", Scope = "World", Period = "Senovė", Title = "Two", DisplayDate = "50 BC", Year = -50 },
        new HistoryEvent { Id = "world-3", Scope = "World", Period = "Senovė", Title = "Three", DisplayDate = "10 BC", Year = -10 },
        new HistoryEvent { Id = "world-4", Scope = "World", Period = "Senovė", Title = "Four", DisplayDate = "100 AD", Year = 100 }
    ];

    private sealed class StubHistoryRepository(IReadOnlyList<HistoryEvent> events) : IHistoryRepository
    {
        public IReadOnlyList<HistoryEvent> GetEvents() => events;
    }
}

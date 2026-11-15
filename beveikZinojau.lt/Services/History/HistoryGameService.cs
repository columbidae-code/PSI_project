using beveikZinojau.lt.Data;
using beveikZinojau.lt.Models;
using beveikZinojau.lt.Scoring;
using Microsoft.Extensions.Caching.Memory;
using beveikZinojau.lt.Services.Geography;

namespace beveikZinojau.lt.Services.History;

public record HistorySessionCreateRequest(
    int QuestionCount,
    string Scope,        // "World", "Lithuania", or "All"
    string Period,       // "Senovė", "Viduramžiai", "Naujieji amžiai", "Modernybė", or "All"
    DifficultyLevel Difficulty
);

public record HistorySessionCreated(
    Guid SessionId,
    int QuestionCount,
    string Scope,
    string Period,
    DifficultyLevel Difficulty,
    Guid PlayerId
);

public record HistoryTimelineIssued(
    Guid QuestionId,
    int QuestionNumber,
    int TotalQuestions,
    double TimeLimitSeconds,
    IReadOnlyList<TimelineCardDto> Cards
);

public record HistoryAnswerRequest(
    Guid PlayerId,
    Guid QuestionId,
    IReadOnlyList<string> OrderedEventIds
);

public record HistoryAnswerChecked(
    bool IsCorrect,
    IReadOnlyList<TimelineCardRevealedDto> CorrectTimeline,
    int PointsEarned,
    int TotalPoints
);

public enum HistoryGameError
{
    None,
    SessionNotFound,
    PlayerNotFound,
    QuestionNotFound,
    WrongSession,
    InvalidSubmission,
    AlreadyAnswered,
    RoundNotFinished
}

public class HistoryGameService
{
    private readonly IMemoryCache cache;
    private readonly HistoryQuestionGenerator generator;
    private readonly ScoringCalculator scoring;
    private readonly StatsTracker statsTracker;

    public HistoryGameService(
        IMemoryCache cache,
        HistoryQuestionGenerator generator,
        ScoringCalculator scoring,
        StatsTracker statsTracker)
    {
        this.cache = cache;
        this.generator = generator;
        this.scoring = scoring;
        this.statsTracker = statsTracker;
    }

    public (HistorySessionCreated? Session, string? Error) CreateSession(
        HistorySessionCreateRequest request,
        string? userId = null)
    {
        if (request.QuestionCount <= 0)
            return (null, "Question count must be positive.");

        string scope = string.IsNullOrWhiteSpace(request.Scope) ? "All" : request.Scope.Trim();
        string period = string.IsNullOrWhiteSpace(request.Period) ? "All" : request.Period.Trim();

        Guid sessionId = Guid.NewGuid();
        var session = new HistorySession(
            questionCount: request.QuestionCount,
            scope: scope,
            period: period,
            difficulty: request.Difficulty,
            userId: userId
        );

        cache.Set($"history-session:{sessionId}", session, TimeSpan.FromMinutes(30));
        Guid playerId = session.AddPlayer();

        return (new HistorySessionCreated(
            sessionId,
            session.QuestionCount,
            session.Scope,
            session.Period,
            session.Difficulty,
            playerId
        ), null);
    }

    public (HistoryTimelineIssued? Question, bool SessionMissing) GetQuestion(Guid sessionId)
    {
        if (!cache.TryGetValue<HistorySession>($"history-session:{sessionId}", out var session) || session is null)
            return (null, true);

        if (session.IsFull)
            return (null, false);

        // Try a few times to avoid issuing the exact same set of cards twice in one session
        for (int attempt = 0; attempt < 10; attempt++)
        {
            var generated = generator.GenerateTimeline(session.Scope, session.Period, session.Difficulty);
            if (generated is null)
                return (null, false);

            if (!session.TryRecordQuestion(generated.SignatureKey, out int number))
                continue;

            Guid questionId = Guid.NewGuid();
            var rule = HistoryQuestionRules.For(session.Difficulty);

            var stored = new StoredHistoryTimeline
            {
                SessionId = sessionId,
                Difficulty = session.Difficulty,
                CorrectEvents = generated.CorrectChronologicalEvents
            };

            cache.Set($"history-question:{questionId}", stored, TimeSpan.FromMinutes(30));

            return (new HistoryTimelineIssued(
                questionId,
                number,
                session.QuestionCount,
                rule.TimeLimitSeconds,
                generated.ShuffledCards
            ), false);
        }

        return (null, false);
    }

    public (HistoryAnswerChecked? Answer, HistoryGameError Error) CheckAnswer(
        Guid sessionId,
        HistoryAnswerRequest request)
    {
        if (!cache.TryGetValue<HistorySession>($"history-session:{sessionId}", out var session) || session is null)
            return (null, HistoryGameError.SessionNotFound);

        if (!session.HasPlayer(request.PlayerId))
            return (null, HistoryGameError.PlayerNotFound);

        if (!cache.TryGetValue<StoredHistoryTimeline>($"history-question:{request.QuestionId}", out var question) || question is null)
            return (null, HistoryGameError.QuestionNotFound);

        if (question.SessionId != sessionId)
            return (null, HistoryGameError.WrongSession);

        if (request.OrderedEventIds is null || request.OrderedEventIds.Count != question.CorrectEvents.Count)
            return (null, HistoryGameError.InvalidSubmission);

        var rule = HistoryQuestionRules.For(question.Difficulty);
        var correctIds = question.CorrectEvents.Select(e => e.Id).ToList();
        bool isCorrect = request.OrderedEventIds.SequenceEqual(correctIds);

        if (!question.TryAnswer(request.PlayerId, isCorrect, rule.TimeLimitSeconds, out double answerSeconds))
            return (null, HistoryGameError.AlreadyAnswered);

        int pointsEarned = scoring.Calculate(
            isCorrect,
            rule.Difficulty,
            new AnswerTiming(answerSeconds, rule.TimeLimitSeconds)
        );

        int totalPoints = session.RecordAnswer(request.PlayerId, isCorrect, pointsEarned, answerSeconds);

        var revealed = question.CorrectEvents
            .Select((ev, index) => new TimelineCardRevealedDto(ev.Id, ev.Title, ev.DisplayDate, index))
            .ToList();

        return (new HistoryAnswerChecked(isCorrect, revealed, pointsEarned, totalPoints), HistoryGameError.None);
    }

    public (GeoGameResult? Result, HistoryGameError Error) GetResult(Guid sessionId, Guid playerId)
    {
        if (!cache.TryGetValue<HistorySession>($"history-session:{sessionId}", out var session) || session is null)
            return (null, HistoryGameError.SessionNotFound);

        var stats = session.GetPlayerStats(playerId);
        if (stats is null)
            return (null, HistoryGameError.PlayerNotFound);

        var (answered, correct, points, totalSeconds) = stats.Value;

        if (answered < session.QuestionCount)
            return (null, HistoryGameError.RoundNotFinished);

        if (session.UserId is not null)
        {
            int categoryId = statsTracker.GetOrCreateCategoryId(HistoryQuestionRules.CategoryName);
            statsTracker.RecordGame(new AppGameResults
            {
                UserId = session.UserId,
                CategoryId = categoryId,
                Difficulty = session.Difficulty,
                Score = points,
                QuestionsAnswered = answered,
                CorrectAnswers = correct,
                TotalAnswerTimeSeconds = totalSeconds
            });
        }

        double accuracy = Math.Round((double)correct / answered * 100, 1);

        GeoGameResult result = new(
            session.QuestionCount,
            correct,
            answered - correct,
            accuracy,
            points,
            session.UserId is not null
        );

        return (result, HistoryGameError.None);
    }

}
    // GetResult(...) follows the exact same pattern as GeoGameService.GetResult,
    // passing HistoryQuestionRules.CategoryName and session.Difficulty to statsTracker.RecordGame(...
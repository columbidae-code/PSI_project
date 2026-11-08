using beveikZinojau.lt.Models;
using Microsoft.Extensions.Caching.Memory;
using beveikZinojau.lt.Data;
using beveikZinojau.lt.Scoring;


namespace beveikZinojau.lt.Services.Geography;

public record GeoSessionCreated(
    Guid SessionId,
    int QuestionCount,
    IReadOnlyList<string> QuestionTypes,
    Guid PlayerId
);

public record GeoQuestionIssued(
    Guid QuestionId,
    int QuestionNumber,
    int TotalQuestions,
    string QuestionType,
    double TimeLimitSeconds,
    string Text,
    string? ImageUrl,
    string[] Options
);

public record GeoAnswerChecked(
    bool IsCorrect,
    int CorrectOptionIndex,
    int PointsEarned,
    int TotalPoints
);

public record GeoGameResult(
    int TotalQuestions,
    int CorrectAnswers,
    int WrongAnswers,
    double AccuracyPercent,
    int TotalPoints,
    bool Saved
);

public enum GeoGameError
{
    None,
    SessionNotFound,
    PlayerNotFound,
    QuestionNotFound,
    WrongSession,
    MissingAnswer,
    InvalidOption,
    AlreadyAnswered,
    RoundNotFinished
}

public class GeoGameService
{
    private readonly IMemoryCache cache;
    private readonly GeoQuestionGenerator generator;
    private readonly ScoringCalculator scoring;
    private readonly StatsTracker statsTracker;
    private static readonly HashSet<string> SupportedQuestionTypes = new()
    {
        "capital",
        "flag",
        "map",
        "outline",
        "neighbour"
    };

    public GeoGameService(IMemoryCache cache, GeoQuestionGenerator generator, ScoringCalculator scoring, StatsTracker statsTracker)
    {
        this.cache = cache;
        this.generator = generator;
        this.scoring = scoring;
        this.statsTracker = statsTracker;
    }

    public (GeoSessionCreated? Session, string? Error)
        CreateSession(GeoSessionCreateRequest request, string? userId = null)
    {
        if (request.QuestionCount <= 0)
            return (null, "Question count must be positive.");

        List<string> selectedTypes = (request.QuestionTypes ?? new())
            .Where(type => !string.IsNullOrWhiteSpace(type))
            .Select(type => type.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        if (selectedTypes.Count == 0)
            return (null, "Select at least one question type.");

        if (selectedTypes.Any(type => !SupportedQuestionTypes.Contains(type)))
            return (null, "Unknown question type selected.");

        Guid sessionId = Guid.NewGuid();

        GeoQuestionSession session = new(
            questionCount: request.QuestionCount,
            questionTypes: selectedTypes,
            userId: userId
        );

        cache.Set(
            $"geo-session:{sessionId}",
            session,
            TimeSpan.FromMinutes(30)
        );

        Guid playerId = session.AddPlayer();

        return (new GeoSessionCreated(
            sessionId,
            session.QuestionCount,
            session.QuestionTypes,
            playerId
        ), null);
    }

    public (GeoQuestionIssued? Question, bool SessionMissing)
    GetQuestion(Guid sessionId)
    {
        if (!cache.TryGetValue<GeoQuestionSession>(
            $"geo-session:{sessionId}", out var session)
            || session is null)
        {
            return (null, true);
        }

        if (session.IsFull)
            return (null, false);

        foreach (GeneratedGeoQuestion generated in
            generator.GenerateCandidates(session.QuestionTypes))
        {
            if (!session.TryRecordQuestion(generated.Key, out int number))
            {
                if (session.IsFull)
                    break;

                continue;
            }

            Guid questionId = Guid.NewGuid();

            StoredGeoQuestion stored = new()
            {
                SessionId = sessionId,
                QuestionType = generated.QuestionType,
                Options = generated.Options,
                CorrectOptionIndex = generated.CorrectOptionIndex
            };

            cache.Set(
                $"geo-question:{questionId}",
                stored,
                TimeSpan.FromMinutes(30)
            );

            GeoQuestionIssued question = new(
                questionId,
                number,
                session.QuestionCount,
                generated.QuestionType,
                GeoQuestionRules.For(generated.QuestionType).TimeLimitSeconds,
                generated.Text,
                generated.ImageUrl,
                generated.Options
            );

            return (question, false);
        }

        return (null, false);
    }

    public (GeoAnswerChecked? Answer, GeoGameError Error)
    CheckAnswer(Guid sessionId, GeoAnswerRequest request)
    {
        if (!cache.TryGetValue<GeoQuestionSession>(
            $"geo-session:{sessionId}", out var session)
            || session is null)
        {
            return (null, GeoGameError.SessionNotFound);
        }

        if (!session.HasPlayer(request.PlayerId))
            return (null, GeoGameError.PlayerNotFound);

        if (!cache.TryGetValue<StoredGeoQuestion>(
            $"geo-question:{request.QuestionId}", out var question)
            || question is null)
        {
            return (null, GeoGameError.QuestionNotFound);
        }

        if (question.SessionId != sessionId)
            return (null, GeoGameError.WrongSession);

        if (!request.SelectedOptionIndex.HasValue)
            return (null, GeoGameError.MissingAnswer);

        int selectedIndex = request.SelectedOptionIndex.Value;

        if (selectedIndex < 0 || selectedIndex >= question.Options.Length)
            return (null, GeoGameError.InvalidOption);

        if (!question.TryAnswer(
            request.PlayerId, selectedIndex, out bool isCorrect))
        {
            return (null, GeoGameError.AlreadyAnswered);
        }

        //The server measures the time itself
        double answerSeconds = (DateTime.UtcNow - question.ServedAt).TotalSeconds;
        GeoQuestionRule rule = GeoQuestionRules.For(question.QuestionType);

        int pointsEarned = scoring.Calculate(isCorrect, rule.Difficulty, new AnswerTiming(answerSeconds, rule.TimeLimitSeconds));
        int totalPoints = session.RecordAnswer(request.PlayerId, isCorrect, pointsEarned, answerSeconds);
        
        return (
            new GeoAnswerChecked(isCorrect, question.CorrectOptionIndex, pointsEarned, totalPoints),
            GeoGameError.None
        );
    }

    public (GeoGameResult? Result, GeoGameError Error)
    GetResult(Guid sessionId, Guid playerId)
    {
        if (!cache.TryGetValue<GeoQuestionSession>(
            $"geo-session:{sessionId}", out var session)
            || session is null)
        {
            return (null, GeoGameError.SessionNotFound);
        }

        var stats = session.GetPlayerStats(playerId);

        if (stats is null)
            return (null, GeoGameError.PlayerNotFound);

        var (answered, correct, points, totalSeconds) = stats.Value;

        if (answered < session.QuestionCount)
            return (null, GeoGameError.RoundNotFinished);
        if (session.UserId is not null && session.TryMarkSaved(playerId))
        {
            try
            {
                int categoryId = statsTracker.GetOrCreateCategoryId(
                    GeoQuestionRules.CategoryName);

                statsTracker.RecordGame(new AppGameResults
                {
                    UserId = session.UserId,
                    CategoryId = categoryId,
                    // A round mixes difficulties, so store the hardest selected type
                    Difficulty = session.QuestionTypes.Max(t => GeoQuestionRules.For(t).Difficulty),
                    Score = points,
                    QuestionsAnswered = answered,
                    CorrectAnswers = correct,
                    TotalAnswerTimeSeconds = totalSeconds
                });
            }
            catch
            {
                // Let the next request try saving again
                session.UnmarkSaved(playerId);
                throw;
            }
        }

        double accuracy = Math.Round(
            (double)correct / answered * 100, 1
        );

        GeoGameResult result = new(
            session.QuestionCount,
            correct,
            answered - correct,
            accuracy,
            points,
            session.UserId is not null
        );

        return (result, GeoGameError.None);
    }
}
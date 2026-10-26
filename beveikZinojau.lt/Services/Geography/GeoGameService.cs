using beveikZinojau.lt.Models;
using Microsoft.Extensions.Caching.Memory;

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
    string Text,
    string? ImageUrl,
    string[] Options
);

public record GeoAnswerChecked(
    bool IsCorrect,
    int CorrectOptionIndex
);

public record GeoGameResult(
    int TotalQuestions,
    int CorrectAnswers,
    int WrongAnswers,
    double AccuracyPercent
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

    private static readonly HashSet<string> SupportedQuestionTypes = new()
    {
        "capital",
        "flag",
        "map",
        "outline",
        "neighbour"
    };

    public GeoGameService(IMemoryCache cache, GeoQuestionGenerator generator)
    {
        this.cache = cache;
        this.generator = generator;
    }

    public (GeoSessionCreated? Session, string? Error)
        CreateSession(GeoSessionCreateRequest request)
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
            questionTypes: selectedTypes
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

        session.RecordAnswer(request.PlayerId, isCorrect);

        return (
            new GeoAnswerChecked(isCorrect, question.CorrectOptionIndex),
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

        var (answered, correct) = stats.Value;

        if (answered < session.QuestionCount)
            return (null, GeoGameError.RoundNotFinished);

        double accuracy = Math.Round(
            (double)correct / answered * 100, 1
        );

        GeoGameResult result = new(
            session.QuestionCount,
            correct,
            answered - correct,
            accuracy
        );

        return (result, GeoGameError.None);
    }
}
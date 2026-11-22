using beveikZinojau.lt.Models;
using beveikZinojau.lt.Scoring;
using beveikZinojau.lt.Services.ServiceHelpers;
using beveikZinojau.lt.Services.Geography.GeoGameEnum;
using beveikZinojau.lt.Services.Geography.GeoGameRecords;
using Microsoft.Extensions.Caching.Memory;

namespace beveikZinojau.lt.Services.Geography;

public class GeoAnswerService
{
    private readonly GeoSessionService sessions;
    private readonly CacheStore<StoredGeoQuestion> questions;
    private readonly ScoringCalculator scoring;

    public GeoAnswerService(
        GeoSessionService sessions,
        IMemoryCache cache,
        ScoringCalculator scoring)
    {
        this.sessions = sessions;
        questions = new(cache, "geo-questions");
        this.scoring = scoring;
    }

    public (GeoAnswerChecked? Answer, GeoGameError Error)CheckAnswer(Guid sessionId, GeoAnswerRequest request)
    {
        var session = sessions.GetSession(sessionId);

        if (session is null)
            return (null, GeoGameError.SessionNotFound);

        if (!session.HasPlayer(request.PlayerId))
            return (null, GeoGameError.PlayerNotFound);

        var (question, questionError) = questions.GetOrError(request.QuestionId, GeoGameError.QuestionNotFound);
        if(question is null) return(null, questionError);

        if (question.SessionId != sessionId)
            return (null, GeoGameError.WrongSession);

        if (request.SelectedOptionIndex is int selectedIndex &&
            (selectedIndex < 0 || selectedIndex >= question.Options.Length))
            return (null, GeoGameError.MissingAnswer);

        GeoQuestionRule rule = GeoQuestionRules.For(question.QuestionType);

        if (!question.TryAnswer(
            request.PlayerId,
            request.SelectedOptionIndex,
            rule.TimeLimitSeconds,
            out bool isCorrect,
            out double answerSeconds))
        {
            return (null, GeoGameError.AlreadyAnswered);
        }

        int pointsEarned = scoring.Calculate(
            isCorrect,
            rule.Difficulty,
            new AnswerTiming(answerSeconds, rule.TimeLimitSeconds)
        );

        int totalPoints = session.RecordAnswer(
            request.PlayerId,
            isCorrect,
            pointsEarned,
            answerSeconds
        );

        return (
            new GeoAnswerChecked(isCorrect, question.CorrectOptionIndex, pointsEarned, totalPoints),
            GeoGameError.None
        );
    }
}
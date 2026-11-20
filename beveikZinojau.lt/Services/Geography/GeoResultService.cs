using beveikZinojau.lt.Models;
using beveikZinojau.lt.Data;
using beveikZinojau.lt.Scoring;
using beveikZinojau.lt.Services.Geography.GeoGameEnum;
using beveikZinojau.lt.Services.Geography.GeoGameRecords;

namespace beveikZinojau.lt.Services.Geography;

public class GeoResultService
{
    private readonly GeoSessionService sessions;
    private readonly StatsTracker statsTracker;

    public GeoResultService(
        GeoSessionService sessions,
        StatsTracker statsTracker)
    {
        this.sessions = sessions;
        this.statsTracker = statsTracker;
    }

    public (GeoGameResult? Result, GeoGameError Error)
    GetResult(Guid sessionId, Guid playerId)
    {
        var session = sessions.GetSession(sessionId);

        if (session is null)
            return (null, GeoGameError.SessionNotFound);

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
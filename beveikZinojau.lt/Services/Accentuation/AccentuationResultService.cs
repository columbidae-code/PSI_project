using beveikZinojau.lt.Data;
using beveikZinojau.lt.Scoring;
using beveikZinojau.lt.Services.Accentuation.AccentuationGameEnum;
using beveikZinojau.lt.Services.Accentuation.AccentuationGameRecords;

namespace beveikZinojau.lt.Services.Accentuation;

public class AccentuationResultService
{
    private readonly AccentuationSessionService _sessions;
    private readonly StatsTracker _statsTracker;

    public AccentuationResultService(
        AccentuationSessionService sessions,
        StatsTracker statsTracker)
    {
        _sessions = sessions;
        _statsTracker = statsTracker;
    }

    public async Task<(AccentuationGameResult? Result, AccentuationGameError Error)> GetResultAsync(
        Guid sessionId,
        Guid playerId)
    {
        var session = _sessions.GetSession(sessionId);

        if (session is null)
            return (null, AccentuationGameError.SessionNotFound);

        var stats = session.GetPlayerStats(playerId);
        if (stats is null)
            return (null, AccentuationGameError.PlayerNotFound);

        var (answered, correct, points, totalSeconds) = stats.Value;

        if (answered < session.QuestionCount)
            return (null, AccentuationGameError.RoundNotFinished);

        if (session.UserId is not null && session.TryMarkSaved(playerId))
        {
            try
            {
                int categoryId = await _statsTracker.GetOrCreateCategoryIdAsync(
                    AccentuationQuestionRules.CategoryName);

                await _statsTracker.RecordGameAsync(new AppGameResults
                {
                    UserId = session.UserId,
                    CategoryId = categoryId,
                    Difficulty = session.Levels.Max(AccentuationQuestionRules.GetDifficultyForLevel),
                    Score = points,
                    QuestionsAnswered = answered,
                    CorrectAnswers = correct,
                    TotalAnswerTimeSeconds = totalSeconds
                });
            }
            catch
            {
                session.UnmarkSaved(playerId);
                throw;
            }
        }

        double accuracy = Math.Round((double)correct / answered * 100, 1);

        AccentuationGameResult result = new(
            session.QuestionCount,
            correct,
            answered - correct,
            accuracy,
            points,
            session.UserId is not null
        );

        return (result, AccentuationGameError.None);
    }
}
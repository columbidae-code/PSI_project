using beveikZinojau.lt.Models;
using beveikZinojau.lt.Scoring;
using beveikZinojau.lt.Services.Accentuation.AccentuationGameEnum;
using beveikZinojau.lt.Services.Accentuation.AccentuationGameRecords;
using beveikZinojau.lt.Services.ServiceHelpers;
using Microsoft.Extensions.Caching.Memory;

namespace beveikZinojau.lt.Services.Accentuation;

public class AccentuationAnswerService
{
    private readonly AccentuationSessionService _sessions;
    private readonly CacheStore<StoredAccentuationQuestion> _questions;
    private readonly ScoringCalculator _scoring;

    public AccentuationAnswerService(
        AccentuationSessionService sessions,
        IMemoryCache cache,
        ScoringCalculator scoring)
    {
        _sessions = sessions;
        _questions = new(cache, "acc-question");
        _scoring = scoring;
    }

    public (AccentuationAnswerChecked? Answer, AccentuationGameError Error) CheckAnswer(
        Guid sessionId,
        AccentuationAnswerRequest request)
    {
        var session = _sessions.GetSession(sessionId);

        if (session is null)
            return (null, AccentuationGameError.SessionNotFound);

        if (!session.HasPlayer(request.PlayerId))
            return (null, AccentuationGameError.PlayerNotFound);

        var (question, questionError) = _questions.GetOrError(
            request.QuestionId,
            AccentuationGameError.QuestionNotFound);

        if (question is null)
            return (null, questionError);

        if (question.SessionId != sessionId)
            return (null, AccentuationGameError.WrongSession);

        if (!request.SelectedPosition.HasValue || string.IsNullOrWhiteSpace(request.SelectedAccentType))
            return (null, AccentuationGameError.MissingAnswer);

        int selectedPos = request.SelectedPosition.Value;
        string selectedAcc = request.SelectedAccentType.Trim().ToUpperInvariant();

        if (selectedPos < 0 || selectedPos >= question.Word.Length)
            return (null, AccentuationGameError.InvalidOption);

        if (selectedAcc is not ("LEFT" or "RIGHT" or "ACUTE"))
            return (null, AccentuationGameError.InvalidOption);

        if (!question.TryMarkAnswered(request.PlayerId))
            return (null, AccentuationGameError.AlreadyAnswered);

        bool samePosition = selectedPos == question.CorrectPosition;
        bool sameAccent = selectedAcc == question.CorrectAccentType;

        string status;
        bool isCorrect;

        if (samePosition && sameAccent)
        {
            status = "correct";
            isCorrect = true;
        }
        else if (samePosition)
        {
            status = "halfcorrect";
            isCorrect = false;
        }
        else
        {
            status = "incorrect";
            isCorrect = false;
        }

        double answerSeconds = (DateTime.UtcNow - question.ServedAt).TotalSeconds;
        DifficultyLevel difficulty = AccentuationQuestionRules.GetDifficultyForLevel(question.Level);

        int pointsEarned = 0;
        if (status == "correct")
        {
            pointsEarned = _scoring.Calculate(
                true,
                difficulty,
                new AnswerTiming(answerSeconds, AccentuationQuestionRules.DefaultTimeLimitSeconds));
        }
        else if (status == "halfcorrect")
        {
            int fullPoints = _scoring.Calculate(
                true,
                difficulty,
                new AnswerTiming(answerSeconds, AccentuationQuestionRules.DefaultTimeLimitSeconds));
            pointsEarned = fullPoints / 2;
        }

        int totalPoints = session.RecordAnswer(request.PlayerId, isCorrect, pointsEarned, answerSeconds);

        return (
            new AccentuationAnswerChecked(
                status,
                isCorrect,
                question.CorrectPosition,
                question.CorrectAccentType,
                question.AccentedWord,
                question.Rule,
                pointsEarned,
                totalPoints
            ),
            AccentuationGameError.None
        );
    }
}
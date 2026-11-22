using beveikZinojau.lt.Data;
using beveikZinojau.lt.Models;
using beveikZinojau.lt.Scoring;
using beveikZinojau.lt.Services.History.HistoryGameEnum;
using beveikZinojau.lt.Services.History.HistoryGameRecords;
using beveikZinojau.lt.Services.ServiceHelpers;
using Microsoft.Extensions.Caching.Memory;



namespace beveikZinojau.lt.Services.History;



public class HistoryGameService
{
    private readonly CacheStore<HistorySession> sessions;
    private readonly CacheStore<StoredHistoryQuestion> questions;
    private readonly HistoryQuestionGenerator generator;
    private readonly ScoringCalculator scoring;
    private readonly StatsTracker statsTracker;

    public HistoryGameService(
        IMemoryCache cache,
        HistoryQuestionGenerator generator,
        ScoringCalculator scoring,
        StatsTracker statsTracker)
    {
        sessions = new(cache, "history-session");
        questions = new(cache, "history-question");
        this.generator = generator;
        this.scoring = scoring;
        this.statsTracker = statsTracker;
    }

    public (HistorySessionCreated? Session, string? Error) CreateSession(
        HistorySessionCreateRequest request,
        string? userId = null)
    {
        if (request.QuestionCount <= 0)
        {
            return (null, "Question count must be positive.");
        }

        string scope = string.IsNullOrWhiteSpace(request.Scope) ? "All" : request.Scope.Trim();
        string period = string.IsNullOrWhiteSpace(request.Period) ? "All" : request.Period.Trim();


        HistorySession session = new(
            questionCount: request.QuestionCount,
            scope: scope,
            period: period,
            difficulty: request.Difficulty,
            userId: userId
        );

        Guid sessionId = sessions.Add(session);

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

    public (HistoryQuestionIssued? Question, bool SessionMissing) GetQuestion(Guid sessionId)
    {
        var session = sessions.Get(sessionId);
        if (session is null) return (null, true);

        if (session.IsFull)
        {
            return (null, false);
        }

        for (int attempt = 0; attempt < 200; attempt++)
        {
            var generated = generator.GenerateTimeline(session.Scope, session.Period, session.Difficulty);

            if (generated is null)
            {
                return (null, false);
            }

            if (!session.TryRecordQuestion(generated.SignatureKey, out int number))
            {
                if (session.IsFull)
                    break;

                continue;
            }


            StoredHistoryQuestion stored = new()
            {
                SessionId = sessionId,
                Difficulty = session.Difficulty,
                CorrectEvents = generated.CorrectChronologicalEvents
            };

            Guid questionId = questions.Add(stored);

            HistoryTimelineRule rule = HistoryQuestionRules.For(session.Difficulty);

            HistoryQuestionIssued question = new(
                questionId,
                number,
                session.QuestionCount,
                rule.TimeLimitSeconds,
                generated.ShuffledCards
            );

            return (question, false);
        }

        return (null, false);
    }

    public (HistoryAnswerChecked? Answer, HistoryGameError Error) CheckAnswer( Guid sessionId, HistoryAnswerRequest request)
    {
        var (session, sessionError) = sessions.GetOrError(sessionId, HistoryGameError.SessionNotFound);
        if (session is null) return (null, sessionError);

        if (!session.HasPlayer(request.PlayerId))
            return (null, HistoryGameError.PlayerNotFound);

        var (question,questionError) = questions.GetOrError(request.QuestionId, HistoryGameError.QuestionNotFound);
        if (question is null) return (null, questionError);

        if (question.SessionId != sessionId)
            return (null, HistoryGameError.WrongSession);

        if (request.OrderedEventIds is null || request.OrderedEventIds.Count == 0)
            return (null, HistoryGameError.InvalidSubmission);

        HistoryTimelineRule rule = HistoryQuestionRules.For(question.Difficulty);

        if (!question.TryAnswer(
            request.PlayerId,
            request.OrderedEventIds,
            rule.TimeLimitSeconds,
            out bool isCorrect,
            out double answerSeconds))
        {
            return (null, HistoryGameError.AlreadyAnswered);
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

        var revealed = question.CorrectEvents
            .Select((ev, index) => new TimelineCardRevealedDto(ev.Id, ev.Title, ev.DisplayDate, index))
            .ToList();

        return (
            new HistoryAnswerChecked(isCorrect, revealed, pointsEarned, totalPoints),
            HistoryGameError.None
        );
    }

    public async Task<(HistoryGameResult? Result, HistoryGameError Error)> GetResultAsync(Guid sessionId, Guid playerId)
    {
        var (session, sessionError) = sessions.GetOrError(sessionId, HistoryGameError.SessionNotFound);
        if (session is null) return (null,sessionError);

        var stats = session.GetPlayerStats(playerId);

        if (stats is null)
            return (null, HistoryGameError.PlayerNotFound);

        var (answered, correct, points, totalSeconds) = stats.Value;

        if (answered < session.QuestionCount)
            return (null, HistoryGameError.RoundNotFinished);

        if (session.UserId is not null && session.TryMarkSaved(playerId))
        {
            try
            {
                int categoryId = await statsTracker.GetOrCreateCategoryIdAsync(
                    HistoryQuestionRules.CategoryName);

                await statsTracker.RecordGameAsync(new AppGameResults
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
            catch
            {
                session.UnmarkSaved(playerId);
                throw;
            }
        }

        double accuracy = Math.Round(
            (double)correct / answered * 100, 1
        );

        HistoryGameResult result = new(
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
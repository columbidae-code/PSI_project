using beveikZinojau.lt.Models;
using beveikZinojau.lt.Scoring;
using beveikZinojau.lt.Services.Accentuation.AccentuationGameRecords;
using beveikZinojau.lt.Services.ServiceHelpers;
using Microsoft.Extensions.Caching.Memory;

namespace beveikZinojau.lt.Services.Accentuation;

public class AccentuationQuestionService
{
    private readonly AccentuationSessionService _sessions;
    private readonly IAccentuationRepository _repository;
    private readonly CacheStore<StoredAccentuationQuestion> _questions;

    public AccentuationQuestionService(
        AccentuationSessionService sessions,
        IAccentuationRepository repository,
        IMemoryCache cache)
    {
        _sessions = sessions;
        _repository = repository;
        _questions = new(cache, "acc-question");
    }

    public async Task<(AccentuationQuestionIssued? Question, bool SessionMissing)> GetQuestionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = _sessions.GetSession(sessionId);

        if (session is null)
            return (null, true);

        if (session.IsFull)
            return (null, false);

        IReadOnlyList<AccentuationEntry> entries = await _repository.GetEntriesAsync(
            session.Levels,
            cancellationToken);

        foreach (AccentuationEntry entry in entries.OrderBy(_ => session.NextRandom()))
        {
            if (string.IsNullOrWhiteSpace(entry.AccentedWord))
                continue;

            string uniqueKey = $"{entry.Level}:{entry.AccentedWord}";

            if (!session.TryRecordQuestion(uniqueKey, out int number))
            {
                if (session.IsFull)
                    break;
                continue;
            }

            StoredAccentuationQuestion stored = new()
            {
                SessionId = sessionId,
                Level = entry.Level,
                Word = entry.Word,
                AccentedWord = entry.AccentedWord,
                CorrectPosition = entry.Position,
                CorrectAccentType = entry.AccentType,
                Rule = entry.Rule
            };

            Guid questionId = _questions.Add(stored);
            string explanation = entry.Explanations.FirstOrDefault() ?? string.Empty;

            AccentuationQuestionIssued issued = new(
                questionId,
                number,
                session.QuestionCount,
                entry.Level,
                AccentuationQuestionRules.DefaultTimeLimitSeconds,
                entry.Word,
                explanation
            );

            return (issued, false);
        }

        return (null, false);
    }
}
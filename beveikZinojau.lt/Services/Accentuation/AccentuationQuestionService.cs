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

    public (AccentuationQuestionIssued? Question, bool SessionMissing) GetQuestion(Guid sessionId)
    {
        var session = _sessions.GetSession(sessionId);

        if (session is null)
            return (null, true);

        if (session.IsFull)
            return (null, false);

        var candidates = new List<(int Level, AccentuationWord Word)>();
        foreach (int level in session.Levels)
        {
            foreach (var word in _repository.GetWords(level))
            {
                candidates.Add((level, word));
            }
        }

        foreach (var candidate in candidates.OrderBy(_ => session.NextRandom()))
        {
            var accentEntry = candidate.Word.Accents.FirstOrDefault();
            if (accentEntry.Value is null)
                continue;

            string uniqueKey = $"{candidate.Level}:{accentEntry.Key}";

            if (!session.TryRecordQuestion(uniqueKey, out int number))
            {
                if (session.IsFull)
                    break;
                continue;
            }

            StoredAccentuationQuestion stored = new()
            {
                SessionId = sessionId,
                Level = candidate.Level,
                Word = candidate.Word.Word,
                AccentedWord = accentEntry.Key,
                CorrectPosition = accentEntry.Value.Position,
                CorrectAccentType = accentEntry.Value.AccentType,
                Rule = candidate.Word.Rule
            };

            Guid questionId = _questions.Add(stored);
            string explanation = candidate.Word.Explanations.FirstOrDefault() ?? string.Empty;

            AccentuationQuestionIssued issued = new(
                questionId,
                number,
                session.QuestionCount,
                candidate.Level,
                AccentuationQuestionRules.DefaultTimeLimitSeconds,
                candidate.Word.Word,
                explanation
            );

            return (issued, false);
        }

        return (null, false);
    }
}
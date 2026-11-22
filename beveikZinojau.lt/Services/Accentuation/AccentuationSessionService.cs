using beveikZinojau.lt.Models;
using beveikZinojau.lt.Services.Accentuation.AccentuationGameRecords;
using beveikZinojau.lt.Services.ServiceHelpers;
using Microsoft.Extensions.Caching.Memory;

namespace beveikZinojau.lt.Services.Accentuation;

public class AccentuationSessionService
{
    private readonly CacheStore<AccentuationSession> _sessions;

    public AccentuationSessionService(IMemoryCache cache)
    {
        _sessions = new(cache, "acc-session");
    }

    public (AccentuationSessionCreated? Session, string? Error) CreateSession(
        AccentuationSessionCreateRequest request,
        string? userId = null)
    {
        if (request.QuestionCount <= 0)
            return (null, "Question count must be positive.");

        List<int> selectedLevels = (request.Levels ?? new())
            .Where(lvl => lvl is >= 1 and <= 5)
            .Distinct()
            .ToList();

        if (selectedLevels.Count == 0)
            return (null, "Select at least one valid level (1-5).");

        AccentuationSession session = new(
            questionCount: request.QuestionCount,
            levels: selectedLevels,
            userId: userId,
            seed: request.Seed
        );

        Guid sessionId = _sessions.Add(session);
        Guid playerId = session.AddPlayer();

        return (new AccentuationSessionCreated(
            sessionId,
            session.QuestionCount,
            session.Levels,
            playerId
        ), null);
    }

    public AccentuationSession? GetSession(Guid sessionId)
    {
        return _sessions.Get(sessionId);
    }
}
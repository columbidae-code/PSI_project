using beveikZinojau.lt.Models;
using Microsoft.Extensions.Caching.Memory;

namespace beveikZinojau.lt.Services.Geography;

public class GeoSessionService
{
    private readonly IMemoryCache cache;

    private static readonly HashSet<string> SupportedQuestionTypes = new()
    {
        "capital",
        "flag",
        "map",
        "outline",
        "neighbour"
    };

    public GeoSessionService(IMemoryCache cache)
    {
        this.cache = cache;
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

    public GeoQuestionSession? GetSession(Guid sessionId)
    {
        cache.TryGetValue<GeoQuestionSession>(
            $"geo-session:{sessionId}", out var session);

        return session;
    }
}
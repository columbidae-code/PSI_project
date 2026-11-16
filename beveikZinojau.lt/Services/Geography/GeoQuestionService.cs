using beveikZinojau.lt.Models;
using beveikZinojau.lt.Scoring;
using Microsoft.Extensions.Caching.Memory;

namespace beveikZinojau.lt.Services.Geography;

public class GeoQuestionService
{
    private readonly GeoSessionService sessions;
    private readonly GeoQuestionGenerator generator;
    private readonly IMemoryCache cache;

    public GeoQuestionService(
        GeoSessionService sessions,
        GeoQuestionGenerator generator,
        IMemoryCache cache)
    {
        this.sessions = sessions;
        this.generator = generator;
        this.cache = cache;
    }

    public (GeoQuestionIssued? Question, bool SessionMissing)
    GetQuestion(Guid sessionId)
    {
        var session = sessions.GetSession(sessionId);

        if (session is null)
            return (null, true);

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
                QuestionType = generated.QuestionType,
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
                GeoQuestionRules.For(generated.QuestionType).TimeLimitSeconds,
                generated.Text,
                generated.ImageUrl,
                generated.Options
            );

            return (question, false);
        }

        return (null, false);
    }
}
using beveikZinojau.lt.Models;
using beveikZinojau.lt.Scoring;
using beveikZinojau.lt.Services.Geography.GeoGameRecords;
using beveikZinojau.lt.Services.ServiceHelpers;
using Microsoft.Extensions.Caching.Memory;
using beveikZinojau.lt.Exceptions;

namespace beveikZinojau.lt.Services.Geography;

public class GeoQuestionService
{

    private readonly GeoSessionService sessions;
    private readonly GeoQuestionGenerator generator;
    private readonly CacheStore<StoredGeoQuestion> questions;
    private readonly ICountryRepository countryRepository;
    public GeoQuestionService(
        GeoSessionService sessions,
        GeoQuestionGenerator generator,
        IMemoryCache cache,
        ICountryRepository countryRepository)
    {
        this.sessions = sessions;
        this.generator = generator;
        questions = new(cache, "geo-question");
        this.countryRepository = countryRepository;
    }

    public async Task<(GeoQuestionIssued? Question, bool SessionMissing)> GetQuestionAsync(Guid sessionId)
    {
        var session = sessions.GetSession(sessionId);

        if (session is null)
            return (null, true);

        if (session.IsFull)
            return (null, false);

        IReadOnlyList<Country> countries = await countryRepository.GetCountriesAsync();

        if (countries.Count == 0)
        {
            throw new InvalidGameDataException(
                "PostgreSQL Countries",
                "Countries",
                "No countries available to generate questions."
            );
        }

        foreach (GeneratedGeoQuestion generated in generator.GenerateCandidates(session.QuestionTypes, countries))
        {
            if (!session.TryRecordQuestion(generated.Key, out int number))
            {
                if (session.IsFull)
                    break;

                continue;
            }


            StoredGeoQuestion stored = new()
            {
                SessionId = sessionId,
                QuestionType = generated.QuestionType,
                Options = generated.Options,
                CorrectOptionIndex = generated.CorrectOptionIndex
            };

            Guid questionId = questions.Add(stored);


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
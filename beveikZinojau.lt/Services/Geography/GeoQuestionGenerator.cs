using beveikZinojau.lt.Models;

namespace beveikZinojau.lt.Services.Geography;

public sealed record GeneratedGeoQuestion(
    GeoQuestionKey Key,
    string QuestionType,
    string Text,
    string? ImageUrl,
    string[] Options,
    int CorrectOptionIndex);

public sealed class GeoQuestionGenerator
{
    private readonly ICountryRepository countryRepository;

    public GeoQuestionGenerator(ICountryRepository countryRepository)
    {
        this.countryRepository = countryRepository;
    }

    public IEnumerable<GeneratedGeoQuestion> GenerateCandidates(IReadOnlyList<string> questionTypes)
    {
        IReadOnlyList<Country> countries = countryRepository.GetCountries();
        string[] shuffledTypes = questionTypes.ToArray();
        Random.Shared.Shuffle(shuffledTypes);

        foreach (string type in shuffledTypes)
        {
            Country[] eligible = countries.Where(country => type switch
            {
                "capital" => !string.IsNullOrWhiteSpace(country.Capital),
                "neighbour" => country.Neighbours.Count > 0,
                "flag" => !string.IsNullOrWhiteSpace(country.Flag),
                "map" => !string.IsNullOrWhiteSpace(country.Map),
                "outline" => !string.IsNullOrWhiteSpace(country.Outline),
                _ => false
            }).ToArray();

            Random.Shared.Shuffle(eligible);

            foreach (Country country in eligible)
            {
                GeneratedGeoQuestion? question = BuildQuestion(country, type, eligible, countries);
                if (question is not null)
                    yield return question;
            }
        }
    }

    private static GeneratedGeoQuestion? BuildQuestion(
        Country country, string type, IReadOnlyList<Country> eligible,
        IReadOnlyList<Country> countries)
    {
        IReadOnlyList<Country> optionCountries = type == "capital" ? eligible : countries;
        List<Country> candidates = optionCountries
            .Where(candidate => candidate.Iso2 != country.Iso2).ToList();
        List<Country> sameRegion = candidates
            .Where(candidate => candidate.Region == country.Region).ToList();
        List<Country> sameContinent = candidates
            .Where(candidate => candidate.Continent == country.Continent).ToList();

        string correctAnswer = type switch
        {
            "capital" => country.Capital!.Trim(),
            "neighbour" => country.Neighbours[Random.Shared.Next(country.Neighbours.Count)],
            _ => country.Name.Trim()
        };

        List<string> answers = new() { correctAnswer };
        if (type == "neighbour")
        {
            // Every wrong option must NOT be a real neighbour.
            AddAnswerOptions(answers, sameRegion.Where(c => !IsNeighbour(country, c)), type);
            AddAnswerOptions(answers, sameContinent.Where(c => !IsNeighbour(country, c)), type);
            AddAnswerOptions(answers, candidates.Where(c => !IsNeighbour(country, c)), type);
        }
        else
        {
            AddAnswerOptions(answers, sameRegion, type);
            AddAnswerOptions(answers, sameContinent, type);
            AddAnswerOptions(answers, candidates, type);
        }

        if (answers.Count < 4)
            return null;

        string[] options = answers.ToArray();
        Random.Shared.Shuffle(options);

        (string text, string? imageUrl) = type switch
        {
            "capital" => ($"Valstybė: {country.Name}. Kokia jos sostinė?", null),
            "flag" => ("Kuriai šaliai priklauso ši vėliava?", country.Flag),
            "map" => ("Kuri šalis pažymėta žemėlapyje?", country.Map),
            "outline" => ("Kurios šalies kontūras pavaizduotas?", country.Outline),
            _ => ($"Valstybė: {country.Name}. Kuri iš šių šalių su ja ribojasi?", null)
        };

        return new GeneratedGeoQuestion(
            new GeoQuestionKey(country.Iso2, type), type, text,
            imageUrl, options, Array.IndexOf(options, correctAnswer));
    }

    private static bool IsNeighbour(Country country, Country candidate) =>
        country.Neighbours.Contains(candidate.Name, StringComparer.OrdinalIgnoreCase);

    private static void AddAnswerOptions(
        List<string> answers, IEnumerable<Country> candidates, string type)
    {
        Country[] shuffled = candidates.ToArray();
        Random.Shared.Shuffle(shuffled);

        foreach (Country candidate in shuffled)
        {
            if (answers.Count >= 4)
                return;

            string option = type == "capital" ? candidate.Capital!.Trim() : candidate.Name.Trim();
            if (!answers.Contains(option, StringComparer.OrdinalIgnoreCase))
                answers.Add(option);
        }
    }
}

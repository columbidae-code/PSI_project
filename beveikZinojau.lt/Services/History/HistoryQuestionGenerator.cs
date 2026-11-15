using beveikZinojau.lt.Models;
using beveikZinojau.lt.Scoring;

namespace beveikZinojau.lt.Services.History;

public sealed record TimelineCardDto(
    string Id,
    string Title
);

public sealed record TimelineCardRevealedDto(
    string Id,
    string Title,
    string DisplayDate,
    int CorrectPosition
);

public sealed record GeneratedHistoryTimeline(
    string SignatureKey,
    IReadOnlyList<TimelineCardDto> ShuffledCards,
    IReadOnlyList<HistoryEvent> CorrectChronologicalEvents
);

public sealed class HistoryQuestionGenerator
{
    private readonly IHistoryRepository historyRepository;

    public HistoryQuestionGenerator(IHistoryRepository historyRepository)
    {
        this.historyRepository = historyRepository;
    }

    public GeneratedHistoryTimeline? GenerateTimeline(
        string scope,
        string period,
        DifficultyLevel difficulty)
    {
        var rule = HistoryQuestionRules.For(difficulty);
        IReadOnlyList<HistoryEvent> allEvents = historyRepository.GetEvents();

        // 1. Filter by Scope ("World", "Lithuania", or "All") and Period
        var filtered = allEvents.Where(e =>
            (scope.Equals("All", StringComparison.OrdinalIgnoreCase) ||
             e.Scope.Equals(scope, StringComparison.OrdinalIgnoreCase)) &&
            (period.Equals("All", StringComparison.OrdinalIgnoreCase) ||
             e.Period.Equals(period, StringComparison.OrdinalIgnoreCase)));

        // 2. Shuffle randomly, then prioritize "World" over "Lithuania" on identical dates
        //    so if World & Lithuania overlap on a date, the 2nd (Lithuania) date is excluded.
        var prioritized = filtered
            .OrderBy(_ => Random.Shared.Next())
            .OrderBy(e => e.Scope.Equals("World", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ToList();

        // 3. Deduplicate by date key (ExactDate for Modernybė, Year for older periods)
        var seenDateKeys = new HashSet<string>();
        var selected = new List<HistoryEvent>();

        foreach (var ev in prioritized)
        {
            string dateKey = ev.ExactDate ?? ev.Year.ToString();

            if (seenDateKeys.Add(dateKey))
            {
                selected.Add(ev);
                if (selected.Count == rule.EventCount)
                    break;
            }
        }

        if (selected.Count < rule.EventCount)
            return null;

        // 4. Sort chronologically for ground truth
        var chronological = selected
            .OrderBy(e => e.Year)
            .ThenBy(e => e.ExactDate ?? string.Empty)
            .ToList();

        // 5. Shuffle for the player (ensure it isn't already accidentally in exact order)
        var shuffled = chronological.ToArray();
        do
        {
            Random.Shared.Shuffle(shuffled);
        } while (shuffled.Select(e => e.Id).SequenceEqual(chronological.Select(e => e.Id)));

        var cards = shuffled
            .Select(e => new TimelineCardDto(e.Id, e.Title))
            .ToList();

        string signatureKey = string.Join("|", chronological.Select(e => e.Id));

        return new GeneratedHistoryTimeline(signatureKey, cards, chronological);
    }
}
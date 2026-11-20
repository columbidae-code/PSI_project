using beveikZinojau.lt.Models;

namespace beveikZinojau.lt.Services.History.HistoryGameRecords
{
    public sealed record GeneratedHistoryTimeline(
        string SignatureKey,
        IReadOnlyList<TimelineCardDto> ShuffledCards,
        IReadOnlyList<HistoryEvent> CorrectChronologicalEvents
    );
}
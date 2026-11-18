namespace beveikZinojau.lt.Services.History.HistoryGameRecords
{
    public sealed record TimelineCardRevealedDto(
        string Id,
        string Title,
        string DisplayDate,
        int CorrectPosition
    );
}
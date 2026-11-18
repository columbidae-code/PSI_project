namespace beveikZinojau.lt.Services.History.HistoryGameRecords
{
    public record HistoryAnswerChecked(
        bool IsCorrect,
        IReadOnlyList<TimelineCardRevealedDto> CorrectTimeline,
        int PointsEarned,
        int TotalPoints
    );
}
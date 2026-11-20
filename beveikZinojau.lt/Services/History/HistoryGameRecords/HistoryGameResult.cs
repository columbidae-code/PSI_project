namespace beveikZinojau.lt.Services.History.HistoryGameRecords
{
    public record HistoryGameResult(
        int TotalQuestions,
        int CorrectAnswers,
        int WrongAnswers,
        double AccuracyPercent,
        int TotalPoints,
        bool Saved
    );
}
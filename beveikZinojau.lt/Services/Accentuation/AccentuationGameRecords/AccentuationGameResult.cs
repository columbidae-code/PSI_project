namespace beveikZinojau.lt.Services.Accentuation.AccentuationGameRecords
{
    public record AccentuationGameResult(
        int TotalQuestions,
        int CorrectAnswers,
        int WrongAnswers,
        double AccuracyPercent,
        int TotalPoints,
        bool Saved
    );
}
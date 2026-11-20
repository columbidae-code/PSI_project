namespace beveikZinojau.lt.Services.Accentuation.AccentuationGameRecords
{
    public record AccentuationAnswerChecked(
        string Status, // "correct", "halfcorrect", or "incorrect"
        bool IsCorrect,
        int CorrectPosition,
        string CorrectAccentType,
        string AccentedWord,
        string Rule,
        int PointsEarned,
        int TotalPoints
    );
}
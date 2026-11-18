namespace beveikZinojau.lt.Services.Geography.GeoGameRecords
{

public record GeoGameResult(
    int TotalQuestions,
    int CorrectAnswers,
    int WrongAnswers,
    double AccuracyPercent,
    int TotalPoints,
    bool Saved
);
}

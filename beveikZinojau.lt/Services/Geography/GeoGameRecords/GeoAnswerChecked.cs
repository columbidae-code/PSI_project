namespace beveikZinojau.lt.Services.Geography.GeoGameRecords
{

public record GeoAnswerChecked(
    bool IsCorrect,
    int CorrectOptionIndex,
    int PointsEarned,
    int TotalPoints
);
}

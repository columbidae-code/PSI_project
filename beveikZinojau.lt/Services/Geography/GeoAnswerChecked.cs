namespace beveikZinojau.lt.Services.Geography;

public record GeoAnswerChecked(
    bool IsCorrect,
    int CorrectOptionIndex,
    int PointsEarned,
    int TotalPoints
);
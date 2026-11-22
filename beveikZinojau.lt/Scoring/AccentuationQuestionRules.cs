using beveikZinojau.lt.Models;

namespace beveikZinojau.lt.Scoring;

public static class AccentuationQuestionRules
{
    public const string CategoryName = "Accentuation";
    public const double DefaultTimeLimitSeconds = 30.0;

    public static DifficultyLevel GetDifficultyForLevel(int level) => level switch
    {
        1 or 2 => DifficultyLevel.Easy,
        3 or 4 => DifficultyLevel.Medium,
        _ => DifficultyLevel.Hard
    };
}
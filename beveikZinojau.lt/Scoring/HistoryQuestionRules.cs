using beveikZinojau.lt.Models;

namespace beveikZinojau.lt.Scoring
{
    public readonly record struct HistoryTimelineRule(
        DifficultyLevel Difficulty,
        int EventCount,
        double TimeLimitSeconds);

    public static class HistoryQuestionRules
    {
        public const string CategoryName = "Istorija";

        public static HistoryTimelineRule For(DifficultyLevel difficulty)
        {
            return difficulty switch
            {
                DifficultyLevel.Easy => new(DifficultyLevel.Easy, EventCount: 4, TimeLimitSeconds: 30),
                DifficultyLevel.Medium => new(DifficultyLevel.Medium, EventCount: 6, TimeLimitSeconds: 45),
                DifficultyLevel.Hard => new(DifficultyLevel.Hard, EventCount: 8, TimeLimitSeconds: 60),
                _ => new(DifficultyLevel.Medium, EventCount: 5, TimeLimitSeconds: 40)
            };
        }
    }
}
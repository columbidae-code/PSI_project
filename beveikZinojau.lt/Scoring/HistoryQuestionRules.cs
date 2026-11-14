using beveikZinojau.lt.Models;

namespace beveikZinojau.lt.Scoring
{
    public readonly record struct HistoryTimelineRule(
        int EventCount,
        double TimeLimitSeconds);

    public static class HistoryQuestionRules
    {
        public const string CategoryName = "Istorija";

        public static HistoryTimelineRule For(DifficultyLevel difficulty)
        {
            return difficulty switch
            {
                DifficultyLevel.Easy => new(EventCount: 4, TimeLimitSeconds: 30),
                DifficultyLevel.Medium => new(EventCount: 6, TimeLimitSeconds: 45),
                DifficultyLevel.Hard => new(EventCount: 8, TimeLimitSeconds: 60),
                _ => new(EventCount: 5, TimeLimitSeconds: 40)
            };
        }
    }
}
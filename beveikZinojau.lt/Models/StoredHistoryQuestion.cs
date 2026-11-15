using beveikZinojau.lt.Models;

namespace beveikZinojau.lt.Models
{
    public class StoredHistoryQuestion
    {
        private readonly object sync = new();
        private readonly HashSet<Guid> answeredPlayers = new();

        public Guid SessionId { get; init; }
        public DifficultyLevel Difficulty { get; init; }
        public IReadOnlyList<HistoryEvent> CorrectEvents { get; init; } = Array.Empty<HistoryEvent>();

        public DateTime ServedAt { get; init; } = DateTime.UtcNow;

        public bool TryAnswer(
            Guid playerId,
            IReadOnlyList<string>? orderedEventIds,
            double timeLimitSeconds,
            out bool isCorrect,
            out double answerSeconds)
        {
            lock (sync)
            {
                if (!answeredPlayers.Add(playerId))
                {
                    isCorrect = false;
                    answerSeconds = 0;
                    return false;
                }

                double elapsed = Math.Max(0, (DateTime.UtcNow - ServedAt).TotalSeconds);
                answerSeconds = Math.Min(elapsed, timeLimitSeconds);

                // Check if submission matches the correct chronological order within time limit
                if (orderedEventIds is null || 
                    orderedEventIds.Count != CorrectEvents.Count || 
                    elapsed > timeLimitSeconds)
                {
                    isCorrect = false;
                }
                else
                {
                    var correctIds = CorrectEvents.Select(e => e.Id).ToList();
                    isCorrect = orderedEventIds.SequenceEqual(correctIds);
                }

                return true;
            }
        }
    }
}
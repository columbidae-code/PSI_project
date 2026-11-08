
using System.Globalization;

namespace beveikZinojau.lt.Models
{
    public class StoredGeoQuestion
    {
        private readonly object sync = new();
        private readonly HashSet<Guid> answeredPlayers = new();

        public Guid SessionId { get; init; }

        public string QuestionType { get; init; } = string.Empty;
        public string[] Options { get; init; } = Array.Empty<string>();
        public int CorrectOptionIndex { get; init; }


        public DateTime ServedAt { get; init; } = DateTime.UtcNow;
        public bool TryAnswer(
            Guid playerId,
            int selectedOptionIndex,
            out bool isCorrect)
        {
            lock (sync)
            {
                if (!answeredPlayers.Add(playerId))
                {
                    isCorrect = false;
                    return false;
                }

                isCorrect = selectedOptionIndex == CorrectOptionIndex;
                return true;
            }
        }
    }
}

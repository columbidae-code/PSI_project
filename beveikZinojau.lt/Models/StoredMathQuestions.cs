using System;
using System.Collections.Generic;
using System.Text;

namespace beveikZinojau.lt.Models
{
    public class StoredMathQuestion
    {
        private readonly object sync = new();
        private readonly HashSet<Guid> answeredPlayers = new();

        public Guid SessionId { get; init; }

        public string QuestionType { get; init; } = string.Empty;
        public int CorrectAnswer { get; init; }


        public DateTime ServedAt { get; init; } = DateTime.UtcNow;
        public bool TryAnswer(
        Guid playerId,
        int? enteredAnswer,
        out bool isCorrect,
        out double answerSeconds,
        double timeLimitSeconds)
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

                isCorrect = enteredAnswer.HasValue && elapsed <= timeLimitSeconds && enteredAnswer.Value == CorrectAnswer;
                return true;
            }
        }
    }
}

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
            int enteredAnswer,
            out bool isCorrect)
        {
            lock (sync)
            {
                if (!answeredPlayers.Add(playerId))
                {
                    isCorrect = false;
                    return false;
                }

                isCorrect = enteredAnswer == CorrectAnswer;
                return true;
            }
        }
    }
}

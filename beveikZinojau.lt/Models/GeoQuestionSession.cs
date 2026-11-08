
namespace beveikZinojau.lt.Models
{   
    
    public readonly record struct PlayerStats(
        int Answered,
        int Correct,
        int Points,
        double TotalAnswerTimeSeconds);
    public class GeoQuestionSession
    {
        private readonly object sync = new();
        private readonly HashSet<GeoQuestionKey> usedQuestions = new();

        private int generatedQuestions = 0;


        private readonly Dictionary<Guid, PlayerProgress> players = new();


        public int QuestionCount { get; }
        public IReadOnlyList<string> QuestionTypes { get; }
        public string? UserId { get; }
        public GeoQuestionSession(
            int questionCount,
            List<string> questionTypes,
            string? userId)
        {
            QuestionCount = questionCount;
            QuestionTypes = Array.AsReadOnly(questionTypes.ToArray());
            UserId = userId;
        }

        public bool TryRecordQuestion(
            GeoQuestionKey questionKey,
            out int questionNumber)
        {
            lock (sync)
            {
                if (generatedQuestions >= QuestionCount ||
                    !usedQuestions.Add(questionKey))
                {
                    questionNumber = 0;
                    return false;
                }

                generatedQuestions++;
                questionNumber = generatedQuestions;
                return true;
            }
        }


        public Guid AddPlayer()
        {
            lock (sync)
            {
                Guid playerId = Guid.NewGuid();
                players.Add(playerId, new PlayerProgress());

                return playerId;
            }
        }

        public bool HasPlayer(Guid playerId)
        {
            lock (sync)
            {
                return players.ContainsKey(playerId);
            }
        }

        public int RecordAnswer(Guid playerId, bool isCorrect, int points, double answerTimeSeconds)
        {
            lock (sync)
            {
                PlayerProgress progress = players[playerId];
                progress.RecordAnswer(isCorrect, points, answerTimeSeconds);

                return progress.Points;
            }
        }

        public PlayerStats? GetPlayerStats(Guid playerId)
        {
            lock (sync)
            {
                if (!players.TryGetValue(playerId, out var progress))
                    return null;

                return new PlayerStats (
                    progress.AnsweredQuestions,
                    progress.CorrectAnswers,
                    progress.Points,
                    progress.TotalAnswerTimeSeconds);
            }
        }
        //True only for the first caller
        public bool TryMarkSaved(Guid playerId)
        {
            lock (sync)
            {
                PlayerProgress progress = players[playerId];

                if (progress.Saved)
                    return false;

                progress.Saved = true;
                return true;
            }
        }

        // Used when the database write failed so the save can be retried
        public void UnmarkSaved(Guid playerId)
        {
            lock (sync)
            {
                players[playerId].Saved = false;
            }
        }
        public bool IsFull
        {
            get
            {
                lock (sync)
                    return generatedQuestions >= QuestionCount;
            }
        }

    }
}

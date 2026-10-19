
namespace beveikZinojau.lt.Models
{
    public class GeoQuestionSession
    {
        private readonly object sync = new();
        private readonly HashSet<GeoQuestionKey> usedQuestions = new();

        private int generatedQuestions = 0;


        private readonly Dictionary<Guid, PlayerProgress> players = new();


        public int QuestionCount { get; }
        public IReadOnlyList<string> QuestionTypes { get; }

        public GeoQuestionSession(
            int questionCount,
            List<string> questionTypes)
        {
            QuestionCount = questionCount;
            QuestionTypes = Array.AsReadOnly(questionTypes.ToArray());
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

        public void RecordAnswer(Guid playerId, bool isCorrect)
        {
            lock (sync)
            {
                players[playerId].RecordAnswer(isCorrect);
            }
        }

        public (int Answered, int Correct)? GetPlayerStats(Guid playerId)
        {
            lock (sync)
            {
                if (!players.TryGetValue(playerId, out var progress))
                    return null;

                return (progress.AnsweredQuestions, progress.CorrectAnswers);
            }
        }

    }
}

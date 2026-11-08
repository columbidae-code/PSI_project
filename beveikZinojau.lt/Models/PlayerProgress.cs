
namespace beveikZinojau.lt.Models
{
    public class PlayerProgress
    {
        public int AnsweredQuestions { get; private set; }
        public int CorrectAnswers { get; private set; }
        public int Points { get; private set; }
        
        public bool Saved { get; set; }
        public double TotalAnswerTimeSeconds { get; private set; }
        public void RecordAnswer(bool isCorrect, int points, double answerTimeSeconds)
        {
            AnsweredQuestions++;
            Points += points;
            TotalAnswerTimeSeconds += answerTimeSeconds;

            if (isCorrect)
                CorrectAnswers++;
        }
    }
}

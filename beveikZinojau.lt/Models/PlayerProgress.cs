
namespace beveikZinojau.lt.Models
{
    public class PlayerProgress
    {
        public int AnsweredQuestions { get; private set; }
        public int CorrectAnswers { get; private set; }

        public void RecordAnswer(bool isCorrect)
        {
            AnsweredQuestions++;

            if (isCorrect)
                CorrectAnswers++;
        }
    }
}

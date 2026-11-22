using beveikZinojau.lt.Models;

namespace beveikZinojau.Tests.MathTests
{
    public class StoredMathQuestionTests
    {
        private const double TimeLimit = 15.0;

        private static StoredMathQuestion MakeQuestion(int correctAnswer, double secondsAgoServed = 0)
        {
            // ServedAt can be set directly, so tests never need to wait for time to pass
            return new StoredMathQuestion
            {
                CorrectAnswer = correctAnswer,
                ServedAt = DateTime.UtcNow.AddSeconds(-secondsAgoServed)
            };
        }

        [Fact]
        public void TryAnswer_CorrectAnswerInTime_IsAcceptedAndCorrect()
        {
            StoredMathQuestion question = MakeQuestion(42);

            bool accepted = question.TryAnswer(Guid.NewGuid(), 42, out bool isCorrect, out double seconds, TimeLimit);

            Assert.True(accepted);
            Assert.True(isCorrect);
            Assert.InRange(seconds, 0, TimeLimit);
        }

        [Fact]
        public void TryAnswer_WrongAnswer_IsAcceptedButNotCorrect()
        {
            StoredMathQuestion question = MakeQuestion(42);

            bool accepted = question.TryAnswer(Guid.NewGuid(), 41, out bool isCorrect, out double seconds, TimeLimit);

            Assert.True(accepted);
            Assert.False(isCorrect);
        }

        [Fact]
        public void TryAnswer_NoAnswerEntered_IsNotCorrect()
        {
            StoredMathQuestion question = MakeQuestion(42);

            bool accepted = question.TryAnswer(Guid.NewGuid(), null, out bool isCorrect, out double seconds, TimeLimit);

            Assert.True(accepted);
            Assert.False(isCorrect);
        }

        [Fact]
        public void TryAnswer_CorrectAnswerAfterTimeLimit_IsNotCorrectAndTimeIsCapped()
        {
            StoredMathQuestion question = MakeQuestion(42, secondsAgoServed: 30);

            bool accepted = question.TryAnswer(Guid.NewGuid(), 42, out bool isCorrect, out double seconds, TimeLimit);

            Assert.True(accepted);
            Assert.False(isCorrect);
            Assert.Equal(TimeLimit, seconds);
        }

        [Fact]
        public void TryAnswer_SamePlayerTwice_SecondAnswerIsRejected()
        {
            StoredMathQuestion question = MakeQuestion(42);
            Guid player = Guid.NewGuid();

            bool first = question.TryAnswer(player, 42, out bool firstCorrect, out double firstSeconds, TimeLimit);
            bool second = question.TryAnswer(player, 42, out bool secondCorrect, out double secondSeconds, TimeLimit);

            Assert.True(first);
            Assert.False(second);
            Assert.False(secondCorrect);
            Assert.Equal(0, secondSeconds);
        }

        [Fact]
        public void TryAnswer_TwoDifferentPlayers_BothAreAccepted()
        {
            StoredMathQuestion question = MakeQuestion(42);

            bool first = question.TryAnswer(Guid.NewGuid(), 42, out bool firstCorrect, out double firstSeconds, TimeLimit);
            bool second = question.TryAnswer(Guid.NewGuid(), 42, out bool secondCorrect, out double secondSeconds, TimeLimit);

            Assert.True(first);
            Assert.True(second);
            Assert.True(firstCorrect);
            Assert.True(secondCorrect);
        }
    }
}

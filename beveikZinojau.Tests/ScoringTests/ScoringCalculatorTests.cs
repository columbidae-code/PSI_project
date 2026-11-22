using beveikZinojau.lt.Models;
using beveikZinojau.lt.Scoring;

namespace beveikZinojau.Tests.ScoringTests
{
    public class ScoringCalculatorTests
    {
        private readonly ScoringCalculator calculator = new ScoringCalculator(new ScoringConfig());

        [Theory]
        [InlineData(DifficultyLevel.Easy)]
        [InlineData(DifficultyLevel.Medium)]
        [InlineData(DifficultyLevel.Hard)]
        public void Calculate_WrongAnswer_ReturnsZero(DifficultyLevel difficulty)
        {
            AnswerTiming timing = new AnswerTiming(3.0, 5.0);

            int points = calculator.Calculate(false, difficulty, timing);

            Assert.Equal(0, points);
        }

        [Theory]
        [InlineData(DifficultyLevel.Easy)]
        [InlineData(DifficultyLevel.Medium)]
        [InlineData(DifficultyLevel.Hard)]
        public void Calculate_CorrectAnswerOutOfTime_ReturnsZero(DifficultyLevel difficulty)
        {
            AnswerTiming timing = new AnswerTiming(6.0, 5.0);

            int points = calculator.Calculate(true, difficulty, timing);

            Assert.Equal(0, points);
        }

        [Theory]
        [InlineData(DifficultyLevel.Easy, 100)]
        [InlineData(DifficultyLevel.Medium, 150)]
        [InlineData(DifficultyLevel.Hard, 200)]
        public void Calculate_InstantCorrectAnswer_ScalesWithDifficulty(DifficultyLevel difficulty, int expectedPoints)
        {
            AnswerTiming timing = new AnswerTiming(0.0, 5.0);

            int points = calculator.Calculate(true, difficulty, timing);

            Assert.Equal(expectedPoints, points);
        }

        [Theory]
        [InlineData(DifficultyLevel.Easy, 60)]
        [InlineData(DifficultyLevel.Medium, 90)]
        [InlineData(DifficultyLevel.Hard, 120)]
        public void Calculate_SlowerCorrectAnswer_ReturnsReducedPoints(DifficultyLevel difficulty, int expectedPoints)
        {
            AnswerTiming timing = new AnswerTiming(4.0, 5.0);

            int points = calculator.Calculate(true, difficulty, timing);

            Assert.Equal(expectedPoints, points);
        }

        [Theory]
        [InlineData(DifficultyLevel.Easy, 50)]
        [InlineData(DifficultyLevel.Medium, 75)]
        [InlineData(DifficultyLevel.Hard, 100)]
        public void Calculate_AnswerAtTimeLimit_ReturnsHalfPoints(DifficultyLevel difficulty, int expectedPoints)
        {
            AnswerTiming timing = new AnswerTiming(5.0, 5.0);

            int points = calculator.Calculate(true, difficulty, timing);

            Assert.Equal(expectedPoints, points);
        }

        [Fact]
        public void Calculate_CustomConfig_UsesConfiguredBasePoints()
        {
            ScoringCalculator custom = new ScoringCalculator(new ScoringConfig { BasePoints = 50 });
            AnswerTiming timing = new AnswerTiming(0.0, 5.0);

            int points = custom.Calculate(true, DifficultyLevel.Easy, timing);

            Assert.Equal(50, points);
        }

        [Fact]
        public void GetDifficultyMultiplier_UnknownDifficulty_Throws()
        {
            bool threw = false;

            try
            {
                calculator.GetDifficultyMultiplier((DifficultyLevel)99);
            }
            catch (ArgumentOutOfRangeException)
            {
                threw = true;
            }

            Assert.True(threw);
        }

        [Fact]
        public void Calculate_CorrectAnswerUnknownDifficulty_Throws()
        {
            AnswerTiming timing = new AnswerTiming(0.0, 5.0);
            bool threw = false;

            try
            {
                calculator.Calculate(true, (DifficultyLevel)99, timing);
            }
            catch (ArgumentOutOfRangeException)
            {
                threw = true;
            }

            Assert.True(threw);
        }
    }
}
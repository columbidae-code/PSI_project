using System;
using System.Collections.Generic;
using System.Text;
using beveikZinojau.lt.Models;
using beveikZinojau.lt.Scoring;
using beveikZinojau.lt.Services.MathGame;
using beveikZinojau.lt.Services.MathGame.MathRecords;

namespace beveikZinojau.Tests.MathGameTests
{
    public class MathQuestionRulesTests
    {
        [Theory]
        [InlineData("add", DifficultyLevel.Easy, 15.0)]
        [InlineData("sub", DifficultyLevel.Easy, 15.0)]
        [InlineData("mul", DifficultyLevel.Medium, 20.0)]
        [InlineData("div", DifficultyLevel.Hard, 25.0)]
        public void For_KnownType_ReturnsItsRule(string type, DifficultyLevel expectedDifficulty, double expectedLimit)
        {
            MathQuestionRule rule = MathQuestionRules.For(type);

            Assert.Equal(expectedDifficulty, rule.Difficulty);
            Assert.Equal(expectedLimit, rule.TimeLimitSeconds);
        }

        [Theory]
        [InlineData("pow")]
        [InlineData("")]
        [InlineData("ADD")]
        public void For_UnknownType_ReturnsMediumDefault(string type)
        {
            MathQuestionRule rule = MathQuestionRules.For(type);

            Assert.Equal(DifficultyLevel.Medium, rule.Difficulty);
            Assert.Equal(20.0, rule.TimeLimitSeconds);
        }

        [Fact]
        public void CategoryName_IsMatematika()
        {
            Assert.Equal("Matematika", MathQuestionRules.CategoryName);
        }



    }


  
}

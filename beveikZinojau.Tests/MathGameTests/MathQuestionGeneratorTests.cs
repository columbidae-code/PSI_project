using beveikZinojau.lt.Services.MathGame;
using beveikZinojau.lt.Services.MathGame.MathRecords;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace beveikZinojau.Tests.MathGameTests
{
    public class MathQuestionGeneratorTests
    {
        public int Repeats = 200;
        [Theory]
        [InlineData("add")]
        [InlineData("sub")]
        [InlineData("mul")]
        [InlineData("div")]
        public void IsSupported_KnownType_ReturnsTrue(string type)
        {
            Assert.True(MathQuestionGenerator.IsSupported(type));
        }

        [Theory]
        [InlineData("pow")]
        [InlineData("")]
        [InlineData("ADD")]
        [InlineData(" add")]
        public void IsSupported_UnknownType_ReturnsFalse(string type)
        {
            Assert.False(MathQuestionGenerator.IsSupported(type));
        }
        
        //Generator

        [Theory]
        [InlineData("add")]
        [InlineData("sub")]
        [InlineData("mul")]
        [InlineData("div")]
        public void Generate_AnyType_CorrectAnswerMatchesEquation(string type)
        {
            MathQuestionGenerator generator = new MathQuestionGenerator();

            List<string> types = new List<string> { type };

            for (int i = 0; i < Repeats; i++)
            {
                GeneratedMathQuestion question = generator.Generate(types);

                Assert.Equal(type, question.QuestionType);
                Assert.Equal(Solve(question.Text), question.CorrectAnswer);
            }
        }
        [Theory]
        [InlineData("add")]
        [InlineData("sub")]
        [InlineData("mul")]
        [InlineData("div")]
        public void Generate_AnyType_KeyIsTheExpressionAndType(string type)
        {
            MathQuestionGenerator generator = new MathQuestionGenerator();

            List<string> types = new List<string> { type };

            for (int i = 0; i < Repeats; i++)
            {
                GeneratedMathQuestion question = generator.Generate(types);

                Assert.Equal(type, question.Key.QuestionType);
                Assert.Equal(question.Key.CountryIso2 + " = ?", question.Text);
            }
        }

        [Fact]
        public void Generate_Division_AlwaysDividesEvenly()
        {
            MathQuestionGenerator generator = new MathQuestionGenerator();

            List<string> types = new List<string> { "div" };

            for (int i = 0; i < Repeats; i++)
            {
                GeneratedMathQuestion question = generator.Generate(types);
                string[] parts = question.Text.Split(' ');   // "84 ÷ 7 = ?"
                int dividend = int.Parse(parts[0]);
                int divisor = int.Parse(parts[2]);

                Assert.Equal(0, dividend % divisor);
            }
        }

        [Theory]
        [InlineData("add")]
        [InlineData("sub")]
        [InlineData("mul")]
        public void Generate_NonDivision_OperandsAreBetween10And19(string type)
        {
            MathQuestionGenerator generator = new MathQuestionGenerator();

            List<string> types = new List<string> { type };

            for (int i = 0; i < Repeats; i++)
            {
                GeneratedMathQuestion question = generator.Generate(types);
                string[] parts = question.Text.Split(' ');
                int first = int.Parse(parts[0]);
                int second = int.Parse(parts[2]);

                Assert.InRange(first, 10, 19);
                Assert.InRange(second, 10, 19);
            }
        }

        [Fact]
        public void Generate_SeveralTypes_ResultTypeIsOneOfThem()
        {
            MathQuestionGenerator generator = new MathQuestionGenerator();

            List<string> types = new List<string> { "add", "mul" };

            for (int i = 0; i < Repeats; i++)
            {
                GeneratedMathQuestion question = generator.Generate(types);

                Assert.Contains(question.QuestionType, types);
            }
        }

        [Fact]
        public void Generate_SeveralTypes_EventuallyUsesEachType()
        {
            MathQuestionGenerator generator = new MathQuestionGenerator();

            List<string> types = new List<string> { "add", "mul" };
            bool sawAdd = false;
            bool sawMul = false;

            for (int i = 0; i < Repeats; i++)
            {
                GeneratedMathQuestion question = generator.Generate(types);

                if (question.QuestionType == "add") sawAdd = true;
                if (question.QuestionType == "mul") sawMul = true;
            }

            Assert.True(sawAdd);
            Assert.True(sawMul);
        }

        [Fact]
        public void Generate_UnknownType_Throws()
        {
            MathQuestionGenerator generator = new MathQuestionGenerator();

            List<string> types = new List<string> { "pow" };
            bool threw = false;

            try
            {
                generator.Generate(types);
            }
            catch (ArgumentException)
            {
                threw = true;
            }

            Assert.True(threw);
        }

        //helper

        private static int Solve(string text)
        {
            string[] parts = text.Split(' ');
            int first = int.Parse(parts[0]);
            int second = int.Parse(parts[2]);

            switch (parts[1])
            {
                case "+": return first + second;
                case "-": return first - second;
                case "×": return first * second;
                case "÷": return first / second;
                default: throw new InvalidOperationException("Unknown symbol: " + parts[1]);
            }
        }
    }
}

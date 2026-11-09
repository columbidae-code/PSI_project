using System;
using System.Collections.Generic;
using System.Text;

    using beveikZinojau.lt.Models;
    using System;
    using System.Collections.Generic;
    using System.Text;


    namespace beveikZinojau.lt.Scoring
    {
        public readonly record struct MathQuestionRule(
            DifficultyLevel Difficulty,
            double TimeLimitSeconds);

        public static class MathQuestionRules
        {
            public const string CategoryName = "Matematika";

            public static MathQuestionRule For(string questionType)
            {
                return questionType switch
                {
                    "add" => new(DifficultyLevel.Easy, 15),
                    "sub" => new(DifficultyLevel.Easy, 15),
                    "mul" => new(DifficultyLevel.Medium, 20),
                    "div" => new(DifficultyLevel.Hard, 25),
                    _ => new(DifficultyLevel.Medium, 20)
                };
            }
        }
    }




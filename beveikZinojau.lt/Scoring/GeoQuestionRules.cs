using System;
using System.Collections.Generic;
using System.Text;
using beveikZinojau.lt.Models;

namespace beveikZinojau.lt.Scoring
{
    public readonly record struct GeoQuestionRule(
        DifficultyLevel Difficulty,
        double TimeLimitSeconds);

    // Difficulty and time limit for each geography question type.
    // Change the numbers here to tune the game.
    public static class GeoQuestionRules
    {
        public const string CategoryName = "Geografija";

        public static GeoQuestionRule For(string questionType)
        {
            return questionType switch
            {
                "flag" => new(DifficultyLevel.Easy, 15),
                "capital" => new(DifficultyLevel.Easy, 15),
                "neighbour" => new(DifficultyLevel.Medium, 20),
                "map" => new(DifficultyLevel.Medium, 20),
                "outline" => new(DifficultyLevel.Hard, 25),
                _ => new(DifficultyLevel.Medium, 20)
            };
        }
    }
}


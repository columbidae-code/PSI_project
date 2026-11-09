using System;
using System.Collections.Generic;
using System.Text;
using beveikZinojau.lt.Models;

namespace beveikZinojau.lt.Services.MathGame
{
   public sealed record GeneratedMathQuestion(
    GeoQuestionKey Key,
    string QuestionType,
    string Text,
    int CorrectAnswer);

public sealed class MathQuestionGenerator
{
    // Question type -> symbol shown to the player
    private static readonly Dictionary<string, char> Symbols = new()
    {
        ["add"] = '+',
        ["sub"] = '-',
        ["mul"] = '×',
        ["div"] = '÷'
    };

    public static bool IsSupported(string questionType)
    {
        return Symbols.ContainsKey(questionType);
    }

    // Makes one random question of one of the given types
    public GeneratedMathQuestion Generate(IReadOnlyList<string> questionTypes)
    {
        string type = questionTypes[Random.Shared.Next(questionTypes.Count)];

        int firstOperand;
        int secondOperand;

        if (type == "div")
        {
            // Pick the answer first and build the question from it,
            // so the division always comes out even (no 15 ÷ 12).
            int answer = Random.Shared.Next(2, 13);
            secondOperand = Random.Shared.Next(2, 10);
            firstOperand = answer * secondOperand;
        }
        else
        {
            firstOperand = Random.Shared.Next(10, 20);
            secondOperand = Random.Shared.Next(10, 20);
        }

        int correctAnswer = Calculate(type, firstOperand, secondOperand);
        string expression = $"{firstOperand} {Symbols[type]} {secondOperand}";

        // The session uses the key to avoid repeating a question in one round.
        // For math the "id" part of the key is the expression itself.
        GeoQuestionKey key = new(expression, type);

        return new GeneratedMathQuestion(
            key,
            type,
            $"{expression} = ?",
            correctAnswer);
    }

    private static int Calculate(string type, int firstOperand, int secondOperand)
    {
        return type switch
        {
            "add" => firstOperand + secondOperand,
            "sub" => firstOperand - secondOperand,
            "mul" => firstOperand * secondOperand,
            "div" => firstOperand / secondOperand,
            _ => throw new ArgumentException($"Unknown question type: {type}")
        };
    }
}
}

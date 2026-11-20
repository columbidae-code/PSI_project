using beveikZinojau.lt.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace beveikZinojau.lt.Services.MathGame.MathRecords
{
    public sealed record GeneratedMathQuestion(
    GeoQuestionKey Key,
    string QuestionType,
    string Text,
    int CorrectAnswer);
}

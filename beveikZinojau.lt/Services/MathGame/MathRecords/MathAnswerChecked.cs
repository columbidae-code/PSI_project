using System;
using System.Collections.Generic;
using System.Text;

namespace beveikZinojau.lt.Services.MathGame.MathRecords
{
    public record MathAnswerChecked(
    bool IsCorrect,
    int CorrectAnswer,
    int PointsEarned,
    int TotalPoints
);
}

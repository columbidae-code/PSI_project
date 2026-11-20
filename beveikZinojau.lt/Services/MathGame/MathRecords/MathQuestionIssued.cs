using System;
using System.Collections.Generic;
using System.Text;

namespace beveikZinojau.lt.Services.MathGame.MathRecords
{
    public record MathQuestionIssued(
        Guid QuestionId,
        int QuestionNumber,
        int TotalQuestions,
        string Equation,
        double TimeLimitSeconds,
        string Text
    );
}

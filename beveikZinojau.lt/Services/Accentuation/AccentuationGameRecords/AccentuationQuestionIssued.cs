namespace beveikZinojau.lt.Services.Accentuation.AccentuationGameRecords
{

    public record AccentuationQuestionIssued(
        Guid QuestionId,
        int QuestionNumber,
        int TotalQuestions,
        int Level,
        double TimeLimitSeconds,
        string Word,
        string Explanation
    );
}
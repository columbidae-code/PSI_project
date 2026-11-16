namespace beveikZinojau.lt.Services.Geography;

public record GeoQuestionIssued(
    Guid QuestionId,
    int QuestionNumber,
    int TotalQuestions,
    string QuestionType,
    double TimeLimitSeconds,
    string Text,
    string? ImageUrl,
    string[] Options
);
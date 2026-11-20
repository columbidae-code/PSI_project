namespace beveikZinojau.lt.Services.History.HistoryGameRecords
{
    public record HistoryQuestionIssued(
      Guid QuestionId,
      int QuestionNumber,
      int TotalQuestions,
      double TimeLimitSeconds,
      IReadOnlyList<TimelineCardDto> Cards
    );
}
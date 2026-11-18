namespace beveikZinojau.lt.Services.History.HistoryGameRecords
{
    public record HistoryAnswerRequest(
          Guid PlayerId,
          Guid QuestionId,
          IReadOnlyList<string> OrderedEventIds
    );
}
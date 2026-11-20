using beveikZinojau.lt.Models;

namespace beveikZinojau.lt.Services.History.HistoryGameRecords
{
    public record HistorySessionCreated(
      Guid SessionId,
      int QuestionCount,
      string Scope,
      string Period,
      DifficultyLevel Difficulty,
      Guid PlayerId
    );
}
using beveikZinojau.lt.Models;

namespace beveikZinojau.lt.Services.History;

public interface IHistoryRepository
{
    IReadOnlyList<HistoryEvent> GetEvents();
}
using beveikZinojau.lt.Models;

namespace beveikZinojau.lt.Services.History;

public interface IHistoryRepository
{
    Task<IReadOnlyList<HistoryEvent>> GetEventsAsync(CancellationToken cancellationToken = default);
}
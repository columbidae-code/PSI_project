using beveikZinojau.lt.Models;

namespace beveikZinojau.lt.Services.Accentuation;

public interface IAccentuationRepository
{
    Task<IReadOnlyList<AccentuationEntry>> GetEntriesAsync(IReadOnlyCollection<int> levels,CancellationToken cancellationToken = default);
}
using beveikZinojau.lt.Data;
using beveikZinojau.lt.Models;
using Microsoft.EntityFrameworkCore;

namespace beveikZinojau.lt.Services.Accentuation;

public sealed class DbAccentuationRepository : IAccentuationRepository
{
    private readonly AppDbContext db;

    public DbAccentuationRepository(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<IReadOnlyList<AccentuationEntry>> GetEntriesAsync(
    IReadOnlyCollection<int> levels,
    CancellationToken cancellationToken = default)
    {
        List<int> selectedLevels = levels.ToList();

        return await db.AccentuationEntries
            .AsNoTracking()
            .Where(entry => selectedLevels.Contains(entry.Level))
            .ToListAsync(cancellationToken);
    }
}
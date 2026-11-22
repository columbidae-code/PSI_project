using beveikZinojau.lt.Data;
using beveikZinojau.lt.Models;
using Microsoft.EntityFrameworkCore;

namespace beveikZinojau.lt.Services.History;

public sealed class DbHistoryRepository : IHistoryRepository
{
    private readonly AppDbContext db;

    public DbHistoryRepository(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<IReadOnlyList<HistoryEvent>> GetEventsAsync(CancellationToken cancellationToken = default)
    {
        return await db.HistoryEvents.AsNoTracking().ToListAsync(cancellationToken);
    }
}
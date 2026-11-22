using beveikZinojau.lt.Data;
using beveikZinojau.lt.Models;
using Microsoft.EntityFrameworkCore;

namespace beveikZinojau.lt.Services.Geography;

public sealed class DbCountryRepository : ICountryRepository
{
    private readonly AppDbContext db;

    public DbCountryRepository(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<IReadOnlyList<Country>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        return await db.Countries.AsNoTracking().ToListAsync(cancellationToken);
    }
}
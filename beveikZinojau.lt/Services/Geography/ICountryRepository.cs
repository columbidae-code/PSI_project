using beveikZinojau.lt.Models;

namespace beveikZinojau.lt.Services.Geography;

public interface ICountryRepository
{
    Task<IReadOnlyList<Country>> GetCountriesAsync(
        CancellationToken cancellationToken = default);
}
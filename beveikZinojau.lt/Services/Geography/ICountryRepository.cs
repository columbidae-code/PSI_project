using beveikZinojau.lt.Models;

namespace beveikZinojau.lt.Services.Geography;

public interface ICountryRepository
{
    IReadOnlyList<Country> GetCountries();
}
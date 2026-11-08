using beveikZinojau.lt.Models;
using System.Text.Json;

namespace beveikZinojau.lt.Services.Geography;

public class JsonCountryRepository : ICountryRepository
{
    public IReadOnlyList<Country> GetCountries()
    {
        string projectFolder = Directory.GetCurrentDirectory();
        string filePath = Path.Combine(projectFolder, "Data", "Geography", "countries.json");

        using FileStream stream = System.IO.File.OpenRead(filePath);

        JsonSerializerOptions options = new();
        options.PropertyNameCaseInsensitive = true;

        List<Country>? countries = JsonSerializer.Deserialize<List<Country>>(stream, options);

        return countries ?? new List<Country>();
    }
}
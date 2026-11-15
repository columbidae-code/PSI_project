using System.Text.Json;
using beveikZinojau.lt.Models;

namespace beveikZinojau.lt.Services.Accentuation;

public class JsonAccentuationRepository : IAccentuationRepository
{
    public IReadOnlyList<AccentuationWord> GetWords(int level)
    {
        string projectFolder = Directory.GetCurrentDirectory();
        string filePath = Path.Combine(projectFolder, "Data", "Accentuation", $"level{level}.json");

        using FileStream stream = File.OpenRead(filePath);

        JsonSerializerOptions options = new();
        options.PropertyNameCaseInsensitive = true;

        var words = JsonSerializer.Deserialize<Dictionary<string, AccentuationWord>>(stream, options);

        return words?.Values.ToList() ?? new List<AccentuationWord>();
    }
}
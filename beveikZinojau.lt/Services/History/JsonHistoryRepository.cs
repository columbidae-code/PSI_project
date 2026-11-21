using System.Text.Json;
using beveikZinojau.lt.Models;

namespace beveikZinojau.lt.Services.History;

public class JsonHistoryRepository : IHistoryRepository
{
    private readonly Lazy<IReadOnlyList<HistoryEvent>> _cachedEvents = new(LoadEvents);

    public IReadOnlyList<HistoryEvent> GetEvents() => _cachedEvents.Value;

    private static IReadOnlyList<HistoryEvent> LoadEvents()
    {
        string projectFolder = Directory.GetCurrentDirectory();
        string filePath = Path.Combine(projectFolder, "Data", "History", "history_events.json");

        using FileStream stream = File.OpenRead(filePath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        return JsonSerializer.Deserialize<List<HistoryEvent>>(stream, options)
               ?? new List<HistoryEvent>();
    }
}
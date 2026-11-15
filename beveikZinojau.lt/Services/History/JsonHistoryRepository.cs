using System.Text.Json;
using beveikZinojau.lt.Models;

namespace beveikZinojau.lt.Services.History;

public class JsonHistoryRepository : IHistoryRepository
{
    private IReadOnlyList<HistoryEvent>? _cachedEvents;

    public IReadOnlyList<HistoryEvent> GetEvents()
    {
        if (_cachedEvents is not null)
            return _cachedEvents;

        string projectFolder = Directory.GetCurrentDirectory();
        string filePath = Path.Combine(projectFolder, "Data", "History", "history_events.json");

        using FileStream stream = File.OpenRead(filePath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        _cachedEvents = JsonSerializer.Deserialize<List<HistoryEvent>>(stream, options)
                        ?? new List<HistoryEvent>();

        return _cachedEvents;
    }
}
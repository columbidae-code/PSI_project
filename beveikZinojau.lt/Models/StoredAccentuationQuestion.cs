namespace beveikZinojau.lt.Models;

public class StoredAccentuationQuestion
{
    private readonly object _sync = new();
    private readonly HashSet<Guid> _answeredPlayers = new();

    public Guid SessionId { get; init; }
    public int Level { get; init; }
    public string Word { get; init; } = string.Empty;
    public string AccentedWord { get; init; } = string.Empty;
    public int CorrectPosition { get; init; }
    public string CorrectAccentType { get; init; } = string.Empty;
    public string Rule { get; init; } = string.Empty;
    public DateTime ServedAt { get; } = DateTime.UtcNow;

    public bool TryMarkAnswered(Guid playerId)
    {
        lock (_sync)
        {
            return _answeredPlayers.Add(playerId);
        }
    }
}
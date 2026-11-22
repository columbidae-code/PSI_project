public class GameRoom
{
    private readonly object _sync = new();
    private readonly List<string> _players = new();

    public string Code { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;

    public bool TryAddPlayer(string username)
    {
        lock (_sync)
        {
            if (_players.Contains(username))
                return false;

            _players.Add(username);
            return true;
        }
    }

    public bool RemovePlayer(string username)
    {
        lock (_sync)
        {
            return _players.Remove(username);
        }
    }

    public IReadOnlyList<string> GetCurrentPlayers()
    {
        lock (_sync)
        {
            return _players.ToList();
        }
    }
}
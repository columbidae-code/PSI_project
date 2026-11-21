using System.Collections.Concurrent;

public class GameRoomManager
{
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();

    public GameRoom CreateRoom(string category)
    {
        while (true)
        {
            string code = GenerateRoomCode();

            var room = new GameRoom
            {
                Code = code,
                Category = category
            };

            // only succeeds if code wasn't already taken
            if (_rooms.TryAdd(code, room))
                return room;
        }
    }

    public GameRoom? GetRoom(string code)
    {
        _rooms.TryGetValue(code, out var room);
        return room;
    }

    public bool AddPlayer(string code, string username)
    {
        var room = GetRoom(code);

        if (room == null)
            return false;

        return room.TryAddPlayer(username);
    }

    public bool RemovePlayer(string code, string username)
    {
        var room = GetRoom(code);

        if (room == null)
            return false;

        return room.RemovePlayer(username);
    }

    private static string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        return string.Concat(
            Enumerable.Range(0, 6)
                .Select(_ => chars[Random.Shared.Next(chars.Length)])
        );
    }
}
public class GameRoomManager
{
    private readonly Dictionary<string, GameRoom> _rooms = new();

    public GameRoom CreateRoom()
    {
        string code;

        do
        {
            code = GenerateRoomCode();
        }
        while (_rooms.ContainsKey(code));

        var room = new GameRoom
        {
            Code = code
        };

        _rooms.Add(code, room);

        return room;
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

        if (room.Players.Contains(username))
            return false;

        room.Players.Add(username);

        return true;
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
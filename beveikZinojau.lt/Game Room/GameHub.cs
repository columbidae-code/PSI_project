using Microsoft.AspNetCore.SignalR;

public class GameHub : Hub
{
    private readonly GameRoomManager _roomManager;
    private readonly ILogger<GameHub> _logger;
    public GameHub(GameRoomManager roomManager, ILogger<GameHub> logger)
    {
        _roomManager = roomManager;
        _logger = logger;
    }

    public async Task<string> CreateRoom(string username, string category)
    {
        _logger.LogInformation("Creating room for {Username}", username);

        var room = _roomManager.CreateRoom(category);

        _roomManager.AddPlayer(code: room.Code, username: username);

        await Groups.AddToGroupAsync(
            connectionId: Context.ConnectionId,
            groupName: room.Code
        );

        await Clients.Group(room.Code)
            .SendAsync(
                "PlayerJoined",
                username
            );

        _logger.LogInformation("Room created: {RoomCode}", room.Code);

        return room.Code;
    }

    public async Task<object?> JoinRoom(string roomCode, string username)
    {
        roomCode = roomCode.ToUpper();

        _logger.LogInformation("{Username} trying to join {RoomCode}", username, roomCode);

        var room = _roomManager.GetRoom(roomCode);

        if (room == null)
        {
            _logger.LogWarning("Room {RoomCode} not found.", roomCode);

            return null;
        }

        var added = _roomManager.AddPlayer(code: roomCode, username: username);

        if (!added)
        {
            _logger.LogWarning(
                "Player {Username} could not be added to room {RoomCode}.",
                username,
                roomCode
             );

            return null;
        }

        await Groups.AddToGroupAsync(
            connectionId: Context.ConnectionId,
            groupName: roomCode
        );

        await Clients.Group(roomCode)
            .SendAsync(
                "PlayerJoined",
                username
            );

        _logger.LogInformation("{Username} joined {RoomCode}", username, roomCode);

        return new
        {
            room.Code,
            room.Category,
            Players = room.GetCurrentPlayers()
        };
    }

    public async Task StartGame(
    string roomCode,
    int questionCount,
    List<string> questionTypes,
    List<int> levels)
    {
        int seed = Random.Shared.Next();

        await Clients.Group(roomCode).SendAsync(
            "GameStarted",
            questionCount,
            questionTypes,
            levels,
            seed
        );
    }
    public async Task LeaveRoom(string roomCode, string username)
    {
        roomCode = roomCode.ToUpper();

        if (!_roomManager.RemovePlayer(code: roomCode, username: username))
            return;

        await Groups.RemoveFromGroupAsync(
            connectionId: Context.ConnectionId,
            groupName: roomCode
        );

        await Clients.Group(roomCode)
            .SendAsync(
                "PlayerLeft",
                username
            );
    }
}
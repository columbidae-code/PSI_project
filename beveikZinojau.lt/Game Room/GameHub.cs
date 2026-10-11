using Microsoft.AspNetCore.SignalR;

public class GameHub : Hub
{
    private readonly GameRoomManager _roomManager;

    public GameHub(GameRoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public async Task<string> CreateRoom(string username)
    {
        Console.WriteLine(
            $"Creating room for {username}"
        );

        var room = _roomManager.CreateRoom();

        _roomManager.AddPlayer(
            room.Code,
            username
        );

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            room.Code
        );

        await Clients.Group(room.Code)
            .SendAsync(
                "PlayerJoined",
                username
            );

        Console.WriteLine(
            $"Room created: {room.Code}"
        );

        return room.Code;
    }

    public async Task<List<string>?> JoinRoom(
        string roomCode,
        string username)
    {
        roomCode = roomCode.ToUpper();

        Console.WriteLine(
            $"{username} trying to join {roomCode}"
        );

        var room = _roomManager.GetRoom(roomCode);

        if (room == null)
        {
            Console.WriteLine("Room not found.");

            return null;
        }

        var added = _roomManager.AddPlayer(
            roomCode,
            username
        );

        if (!added)
        {
            Console.WriteLine(
                "Player could not be added."
            );

            return null;
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            roomCode
        );

        await Clients.Group(roomCode)
            .SendAsync(
                "PlayerJoined",
                username
            );

        Console.WriteLine(
            $"{username} joined {roomCode}"
        );

        return room.Players;
    }

    public async Task LeaveRoom(
        string roomCode,
        string username)
    {
        var room = _roomManager.GetRoom(roomCode);

        if (room == null)
            return;

        room.Players.Remove(username);

        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            roomCode
        );

        await Clients.Group(roomCode)
            .SendAsync(
                "PlayerLeft",
                username
            );
    }
}
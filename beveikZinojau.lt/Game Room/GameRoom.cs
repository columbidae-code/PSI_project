public class GameRoom
{
    public string Code { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> Players { get; set; } = new();
}
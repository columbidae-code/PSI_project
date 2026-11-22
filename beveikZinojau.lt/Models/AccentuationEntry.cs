namespace beveikZinojau.lt.Models;

public class AccentuationEntry
{
    public int Id { get; set; }
    public int Level { get; set; }

    public string Word { get; set; } = string.Empty;
    public List<string> Explanations { get; set; } = new();
    public string Rule { get; set; } = string.Empty;

    public string AccentedWord { get; set; } = string.Empty;
    public int Position { get; set; }
    public string AccentType { get; set; } = string.Empty;
}
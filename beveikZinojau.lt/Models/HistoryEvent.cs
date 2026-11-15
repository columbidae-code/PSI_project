namespace beveikZinojau.lt.Models;

public class HistoryEvent
{
    public string Id { get; set; } = string.Empty;
    public string Scope { get; set; } = "World";
    public string Period { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string DisplayDate { get; set; } = string.Empty;
    public int Year { get; set; }
    public string? ExactDate { get; set; }
}
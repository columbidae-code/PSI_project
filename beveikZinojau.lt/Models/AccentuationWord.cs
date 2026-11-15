using System.Text.Json.Serialization;

namespace beveikZinojau.lt.Models;

public class AccentuationWord
{
    // Unaccented word displayed to the player
    [JsonPropertyName("wrd")]
    public string Word { get; set; } = string.Empty;

    // Grammar explanations
    [JsonPropertyName("exp")]
    public List<string> Explanations { get; set; } = new();

    // Accentuation rule shown after the player submits an answer
    [JsonPropertyName("aft")]
    public string Rule { get; set; } = string.Empty;

    // Correct answer
    [JsonPropertyName("acc")]
    public Dictionary<string, AccentInfo> Accents { get; set; } = new();
}

public class AccentInfo
{
    // Which letter is accented (starting from zero)
    [JsonPropertyName("pos")]
    public int Position { get; set; }

    // Type of accent (dešininis, kairinis, riestinis)
    [JsonPropertyName("act")]
    public string AccentType { get; set; } = string.Empty;
}
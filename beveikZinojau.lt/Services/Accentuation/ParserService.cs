using System.Text.Encodings.Web;
using System.Text.Json;
using beveikZinojau.lt.Models;
using beveikZinojau.lt.Exceptions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace beveikZinojau.lt.Services.Accentuation;

public class ParserService
{
    // Unicodes for lithuanian accentuation
    private const char RightAccent = '\u0301'; // Acute (dešininis)
    private const char LeftAccent = '\u0300';  // Grave (kairinis)
    private const char TildeAccent = '\u0303'; // Tilde/Circumflex (riestinis)

    // Parses a .docx file and returns a dictionary with indexed words
    public Dictionary<string, AccentuationWord> ParseDocx(string docxFilePath)
    {
        var data = new Dictionary<string, AccentuationWord>();
        int index = 0;

        // Open the Word document in read only mode
        using WordprocessingDocument doc = WordprocessingDocument.Open(docxFilePath, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return data;

        foreach (var table in body.Elements<Table>())
        {
            bool firstRow = true;

            foreach (var row in table.Elements<TableRow>())
            {
                // Skip the header row
                if (firstRow)
                {
                    firstRow = false;
                    continue;
                }

                var cells = row.Elements<TableCell>().ToList();
                if (cells.Count < 3) continue;

                // Column 1: word and grammatrical explanation
                var wordParagraphs = cells[1].Elements<Paragraph>().ToList();
                if (wordParagraphs.Count == 0) continue;

                string rawWordText = wordParagraphs[0].InnerText.Trim();
                if (string.IsNullOrWhiteSpace(rawWordText) || rawWordText == "Žodis")
                    continue; // Skip repeated header rows

                string explanation = "";
                if (wordParagraphs.Count > 1)
                {
                    explanation = wordParagraphs[1].InnerText
                        .Replace("(", "")
                        .Replace(")", "")
                        .Trim();
                }

                // Column 2: accentuation rule
                string rule = string.Join(" ", cells[2]
                    .Elements<Paragraph>()
                    .Select(p => p.InnerText.Trim())
                    .Where(t => !string.IsNullOrEmpty(t)));

                // Extract information in parentheses
                string word = rawWordText;
                string kirciuote = "";

                if (word.Contains('('))
                {
                    var parts = word.Split('(');
                    word = parts[0].Trim();

                    string inParentheses = string.Join("(", parts.Skip(1))
                        .Replace(")", "")
                        .Trim();

                    // If it starts with a digit, treat it as an kirčiuotė;
                    // otherwise treat it as a grammatical explanation
                    if (inParentheses.Length > 0 && char.IsDigit(inParentheses[0]))
                    {
                        kirciuote = FormatParadigm(inParentheses);
                    }
                    else if (string.IsNullOrEmpty(explanation))
                    {
                        explanation = inParentheses;
                    }
                }

                // Find where the accent mark is and calculate its position
                int accentPos;
                string accentType;

                if (word.Contains(RightAccent))
                {
                    accentPos = word.IndexOf(RightAccent) - 1;
                    accentType = "RIGHT";
                }
                else if (word.Contains(LeftAccent))
                {
                    accentPos = word.IndexOf(LeftAccent) - 1;
                    accentType = "LEFT";
                }
                else if (word.Contains(TildeAccent))
                {
                    accentPos = word.IndexOf(TildeAccent) - 1;
                    accentType = "ACUTE";
                }
                else
                {
                    throw new InvalidGameDataException(
                        docxFilePath,
                        word,
                        $"No recognized accent mark found at row {index + 1}.");
                }

                // Remove accent marks to get the unaccented word shown to players
                string unaccentedWord = word
                    .Replace(RightAccent.ToString(), "")
                    .Replace(LeftAccent.ToString(), "")
                    .Replace(TildeAccent.ToString(), "");

                // Formatting the explanation string
                string finalExplanation;
                if (!string.IsNullOrEmpty(kirciuote) && !string.IsNullOrEmpty(explanation))
                    finalExplanation = $"{kirciuote} kirčiuotė, {explanation}";
                else if (!string.IsNullOrEmpty(kirciuote))
                    finalExplanation = $"{kirciuote} kirčiuotė";
                else if (!string.IsNullOrEmpty(explanation))
                    finalExplanation = explanation;
                else
                    finalExplanation = string.Empty;

                data[index.ToString()] = new AccentuationWord
                {
                    Word = unaccentedWord,
                    Explanations = new List<string> { finalExplanation },
                    Rule = rule.Trim(),
                    Accents = new Dictionary<string, AccentInfo>
                    {
                        [word] = new AccentInfo
                        {
                            Position = accentPos,
                            AccentType = accentType
                        }
                    }
                };

                index++;
            }
        }

        return data;
    }


    // Parses a .docx file and writes the data to a JSON file
    public async Task ParseDocxToJsonFileAsync(string docxFilePath, string outputJsonPath)
    {
        var data = ParseDocx(docxFilePath);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            // Preserve Lithuanian characters and Unicode accents
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        string json = JsonSerializer.Serialize(data, options);
        await File.WriteAllTextAsync(outputJsonPath, json);
    }

    // Converts "3a" or "3b" (kirčiuotės) so the letter is displayed as superscript
    private static string FormatParadigm(string paradigm)
    {
        if (paradigm.Length > 1 && paradigm.StartsWith('3'))
        {
            string suffix = paradigm[1..];
            bool isValid = suffix.All(c => c is 'a' or 'b' || char.IsDigit(c));
            if (isValid)
            {
                return $"3<sup>{suffix}</sup>";
            }
        }
        return paradigm;
    }

    public async Task GenerateAllLevelsJsonAsync()
    {
        string folder = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Accentuation");

        for (int level = 1; level <= 5; level++)
        {
            string docxPath = Path.Combine(folder, $"{level}.docx");
            string jsonPath = Path.Combine(folder, $"level{level}.json");

            if (File.Exists(docxPath) && !File.Exists(jsonPath))
            {
                await ParseDocxToJsonFileAsync(docxPath, jsonPath);
            }
        }
    }
}
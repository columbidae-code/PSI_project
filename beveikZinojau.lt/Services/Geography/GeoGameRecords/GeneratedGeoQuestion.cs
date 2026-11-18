using beveikZinojau.lt.Models;

namespace beveikZinojau.lt.Services.Geography.GeoGameRecords
{


public sealed record GeneratedGeoQuestion(
    GeoQuestionKey Key,
    string QuestionType,
    string Text,
    string? ImageUrl,
    string[] Options,
    int CorrectOptionIndex);
}
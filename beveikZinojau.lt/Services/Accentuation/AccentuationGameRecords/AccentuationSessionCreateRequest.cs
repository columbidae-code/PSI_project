namespace beveikZinojau.lt.Services.Accentuation.AccentuationGameRecords
{
    public record AccentuationSessionCreateRequest(
        int QuestionCount,
        List<int> Levels,
        int? Seed = null
    );
}
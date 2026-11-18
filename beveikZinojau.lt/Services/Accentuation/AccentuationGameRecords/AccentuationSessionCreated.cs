namespace beveikZinojau.lt.Services.Accentuation.AccentuationGameRecords
{
    public record AccentuationSessionCreated(
        Guid SessionId,
        int QuestionCount,
        IReadOnlyList<int> Levels,
        Guid PlayerId
    );
}
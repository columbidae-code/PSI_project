namespace beveikZinojau.lt.Services.Geography.GeoGameRecords
{

    public record GeoSessionCreated(
        Guid SessionId,
        int QuestionCount,
        IReadOnlyList<string> QuestionTypes,
        Guid PlayerId
    );
}

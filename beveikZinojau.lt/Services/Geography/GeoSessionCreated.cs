namespace beveikZinojau.lt.Services.Geography;

public record GeoSessionCreated(
    Guid SessionId,
    int QuestionCount,
    IReadOnlyList<string> QuestionTypes,
    Guid PlayerId
);
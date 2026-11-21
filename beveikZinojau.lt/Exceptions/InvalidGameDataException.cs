using Exceptions;

namespace beveikZinojau.lt.Exceptions;

[Serializable]
public class InvalidGameDataException : Exception
{
    public string source;
    public string record;
    public string reason;

    public InvalidGameDataException(string source, string record, string reason) 
        : base($"Invalid game data for record '{record}' from source '{source}': {reason}")
    {
        this.source = source;
        this.record = record;
        this.reason = reason;
    }

}
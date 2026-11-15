public class GameSessionExpiredException : Exception
{
    public Guid SessionId { get; }
    public GameSessionExpiredException(Guid sessionId) 
        : base($"The session {sessionId} has expired or does not exist.")
    {
        SessionId = sessionId;
    }
}
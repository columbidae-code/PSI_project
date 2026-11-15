using beveikZinojau.lt.Models;

namespace beveikZinojau.lt.Services.History;

public class HistorySession
{
    private readonly HashSet<string> _askedSignatures = new();
    private readonly Dictionary<Guid, PlayerSessionStats> _players = new();

    public int QuestionCount { get; }
    public string Scope { get; }
    public string Period { get; }
    public DifficultyLevel Difficulty { get; }
    public string? UserId { get; }

    public bool IsFull => _askedSignatures.Count >= QuestionCount;

    public HistorySession(
        int questionCount,
        string scope,
        string period,
        DifficultyLevel difficulty,
        string? userId)
    {
        QuestionCount = questionCount;
        Scope = scope;
        Period = period;
        Difficulty = difficulty;
        UserId = userId;
    }

    public Guid AddPlayer()
    {
        Guid playerId = Guid.NewGuid();
        _players[playerId] = new PlayerSessionStats();
        return playerId;
    }

    public bool HasPlayer(Guid playerId) => _players.ContainsKey(playerId);

    public bool TryRecordQuestion(string signatureKey, out int questionNumber)
    {
        lock (_askedSignatures)
        {
            if (_askedSignatures.Contains(signatureKey) || _askedSignatures.Count >= QuestionCount)
            {
                questionNumber = 0;
                return false;
            }

            _askedSignatures.Add(signatureKey);
            questionNumber = _askedSignatures.Count;
            return true;
        }
    }

    public int RecordAnswer(Guid playerId, bool isCorrect, int points, double seconds)
    {
        if (_players.TryGetValue(playerId, out var stats))
        {
            stats.AnsweredCount++;
            if (isCorrect) stats.CorrectCount++;
            stats.TotalPoints += points;
            stats.TotalTimeSeconds += seconds;
            return stats.TotalPoints;
        }
        return 0;
    }

    public (int Answered, int Correct, int Points, double TotalSeconds)? GetPlayerStats(Guid playerId)
    {
        if (_players.TryGetValue(playerId, out var stats))
        {
            return (stats.AnsweredCount, stats.CorrectCount, stats.TotalPoints, stats.TotalTimeSeconds);
        }
        return null;
    }

    private class PlayerSessionStats
    {
        public int AnsweredCount { get; set; }
        public int CorrectCount { get; set; }
        public int TotalPoints { get; set; }
        public double TotalTimeSeconds { get; set; }
    }
}

public class StoredHistoryTimeline
{
    public Guid SessionId { get; set; }
    public DifficultyLevel Difficulty { get; set; }
    public IReadOnlyList<HistoryEvent> CorrectEvents { get; set; } = Array.Empty<HistoryEvent>();

    private readonly HashSet<Guid> _answeredPlayers = new();
    private readonly DateTime _issuedAt = DateTime.UtcNow;

    public bool TryAnswer(Guid playerId, bool isCorrect, double timeLimitSeconds, out double answerSeconds)
    {
        lock (_answeredPlayers)
        {
            if (_answeredPlayers.Contains(playerId))
            {
                answerSeconds = 0;
                return false;
            }

            _answeredPlayers.Add(playerId);
            answerSeconds = (DateTime.UtcNow - _issuedAt).TotalSeconds;
            if (answerSeconds > timeLimitSeconds)
                answerSeconds = timeLimitSeconds;
            if (answerSeconds < 0)
                answerSeconds = 0;

            return true;
        }
    }
}
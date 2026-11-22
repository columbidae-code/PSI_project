namespace beveikZinojau.lt.Models;

public class AccentuationSession
{
    private readonly object _sync = new();
    private readonly HashSet<string> _usedQuestions = new();
    private readonly Dictionary<Guid, PlayerProgress> _players = new();
    private readonly Random _rng;
    private int _generatedQuestions = 0;

    public int QuestionCount { get; }
    public IReadOnlyList<int> Levels { get; }
    public string? UserId { get; }

    public AccentuationSession(int questionCount, List<int> levels, string? userId, int? seed = null)
    {
        QuestionCount = questionCount;
        Levels = Array.AsReadOnly(levels.ToArray());
        UserId = userId;
        _rng = new Random(seed ?? Random.Shared.Next());
    }

    public bool TryRecordQuestion(string questionKey, out int questionNumber)
    {
        lock (_sync)
        {
            if (_generatedQuestions >= QuestionCount || !_usedQuestions.Add(questionKey))
            {
                questionNumber = 0;
                return false;
            }

            _generatedQuestions++;
            questionNumber = _generatedQuestions;
            return true;
        }
    }

    public Guid AddPlayer()
    {
        lock (_sync)
        {
            Guid playerId = Guid.NewGuid();
            _players.Add(playerId, new PlayerProgress());
            return playerId;
        }
    }

    public bool HasPlayer(Guid playerId)
    {
        lock (_sync)
        {
            return _players.ContainsKey(playerId);
        }
    }

    public int RecordAnswer(Guid playerId, bool isCorrect, int points, double answerTimeSeconds)
    {
        lock (_sync)
        {
            PlayerProgress progress = _players[playerId];
            progress.RecordAnswer(isCorrect, points, answerTimeSeconds);
            return progress.Points;
        }
    }

    public PlayerStats? GetPlayerStats(Guid playerId)
    {
        lock (_sync)
        {
            if (!_players.TryGetValue(playerId, out var progress))
                return null;

            return new PlayerStats(
                progress.AnsweredQuestions,
                progress.CorrectAnswers,
                progress.Points,
                progress.TotalAnswerTimeSeconds);
        }
    }

    public bool TryMarkSaved(Guid playerId)
    {
        lock (_sync)
        {
            if (!_players.TryGetValue(playerId, out var progress) || progress.Saved)
                return false;

            progress.Saved = true;
            return true;
        }
    }

    public void UnmarkSaved(Guid playerId)
    {
        lock (_sync)
        {
            if (_players.TryGetValue(playerId, out var progress))
                progress.Saved = false;
        }
    }

    public bool IsFull
    {
        get
        {
            lock (_sync)
                return _generatedQuestions >= QuestionCount;
        }
    }

    public int NextRandom()
    {
        lock (_sync)
        {
            return _rng.Next();
        }
    }
}
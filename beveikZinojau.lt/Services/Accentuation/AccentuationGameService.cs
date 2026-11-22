using beveikZinojau.lt.Data;
using beveikZinojau.lt.Models;
using beveikZinojau.lt.Scoring;
using beveikZinojau.lt.Services.Accentuation.AccentuationGameEnum;
using beveikZinojau.lt.Services.Accentuation.AccentuationGameRecords;
using beveikZinojau.lt.Services.ServiceHelpers;
using Microsoft.Extensions.Caching.Memory;

namespace beveikZinojau.lt.Services.Accentuation;

public class AccentuationGameService
{
    public const string CategoryName = "Accentuation";
    private const double DefaultTimeLimitSeconds = 30.0;

    private readonly CacheStore<AccentuationSession> sessions;
    private readonly CacheStore<StoredAccentuationQuestion> questions;
    private readonly IAccentuationRepository repository;
    private readonly ScoringCalculator scoring;
    private readonly StatsTracker statsTracker;

    public AccentuationGameService(
        IMemoryCache cache,
        IAccentuationRepository repository,
        ScoringCalculator scoring,
        StatsTracker statsTracker)
    {
        sessions = new(cache, "acc-session");
        questions = new(cache, "acc-question");
        this.repository = repository;
        this.scoring = scoring;
        this.statsTracker = statsTracker;
    }

    public (AccentuationSessionCreated? Session, string? Error) CreateSession(
        AccentuationSessionCreateRequest request,
        string? userId = null)
    {
        if (request.QuestionCount <= 0)
            return (null, "Question count must be positive.");

        List<int> selectedLevels = (request.Levels ?? new())
            .Where(lvl => lvl is >= 1 and <= 5)
            .Distinct()
            .ToList();

        if (selectedLevels.Count == 0)
            return (null, "Select at least one valid level (1-5).");


        AccentuationSession session = new(
            questionCount: request.QuestionCount,
            levels: selectedLevels,
            userId: userId,
            seed: request.Seed
        );

        Guid sessionId = sessions.Add(session);


        Guid playerId = session.AddPlayer();

        return (new AccentuationSessionCreated(
            sessionId,
            session.QuestionCount,
            session.Levels,
            playerId
        ), null);
    }

    public (AccentuationQuestionIssued? Question, bool SessionMissing) GetQuestion(Guid sessionId)
    {
        var session = sessions.Get(sessionId);
        if (session is null) return (null, true);

        if (session.IsFull)
            return (null, false);

        // Gather all words from the selected levels
        var candidates = new List<(int Level, AccentuationWord Word)>();
        foreach (int level in session.Levels)
        {
            foreach (var word in repository.GetWords(level))
            {
                candidates.Add((level, word));
            }
        }

        // Shuffle candidates randomly
        foreach (var candidate in candidates.OrderBy(_ => session.NextRandom()))
        {
            var accentEntry = candidate.Word.Accents.FirstOrDefault();
            if (accentEntry.Value is null) continue;

            string uniqueKey = $"{candidate.Level}:{accentEntry.Key}";

            if (!session.TryRecordQuestion(uniqueKey, out int number))
            {
                if (session.IsFull)
                    break;
                continue;
            }


            StoredAccentuationQuestion stored = new()
            {
                SessionId = sessionId,
                Level = candidate.Level,
                Word = candidate.Word.Word,
                AccentedWord = accentEntry.Key,
                CorrectPosition = accentEntry.Value.Position,
                CorrectAccentType = accentEntry.Value.AccentType,
                Rule = candidate.Word.Rule
            };

            Guid questionId = questions.Add(stored);

            string explanation = candidate.Word.Explanations.FirstOrDefault() ?? string.Empty;

            AccentuationQuestionIssued issued = new(
                questionId,
                number,
                session.QuestionCount,
                candidate.Level,
                DefaultTimeLimitSeconds,
                candidate.Word.Word,
                explanation
            );

            return (issued, false);
        }

        return (null, false);
    }

    public (AccentuationAnswerChecked? Answer, AccentuationGameError Error) CheckAnswer(
        Guid sessionId,
        AccentuationAnswerRequest request)
    {
        var session = sessions.Get(sessionId);
        if (session is null) return (null, AccentuationGameError.SessionNotFound);

        if (!session.HasPlayer(request.PlayerId))
            return (null, AccentuationGameError.PlayerNotFound);

        var question = questions.Get(request.QuestionId);
        if (question is null) return (null, AccentuationGameError.QuestionNotFound);

        if (question.SessionId != sessionId)
            return (null, AccentuationGameError.WrongSession);

        if (!request.SelectedPosition.HasValue || string.IsNullOrWhiteSpace(request.SelectedAccentType))
            return (null, AccentuationGameError.MissingAnswer);

        int selectedPos = request.SelectedPosition.Value;
        string selectedAcc = request.SelectedAccentType.Trim().ToUpperInvariant();

        if (selectedPos < 0 || selectedPos >= question.Word.Length)
            return (null, AccentuationGameError.InvalidOption);

        if (selectedAcc is not ("LEFT" or "RIGHT" or "ACUTE"))
            return (null, AccentuationGameError.InvalidOption);

        if (!question.TryMarkAnswered(request.PlayerId))
            return (null, AccentuationGameError.AlreadyAnswered);

        // "correct" (green), "halfcorrect" (yellow), or "incorrect" (red)
        bool samePosition = selectedPos == question.CorrectPosition;
        bool sameAccent = selectedAcc == question.CorrectAccentType;

        string status;
        bool isCorrect;

        if (samePosition && sameAccent)
        {
            status = "correct";
            isCorrect = true;
        }
        else if (samePosition)
        {
            status = "halfcorrect";
            isCorrect = false;
        }
        else
        {
            status = "incorrect";
            isCorrect = false;
        }

        double answerSeconds = (DateTime.UtcNow - question.ServedAt).TotalSeconds;
        DifficultyLevel difficulty = GetDifficultyForLevel(question.Level);

        int pointsEarned = 0;
        if (status == "correct")
        {
            pointsEarned = scoring.Calculate(true, difficulty, new AnswerTiming(answerSeconds, DefaultTimeLimitSeconds));
        }
        else if (status == "halfcorrect")
        {
            // Award half points if the player chose the right letter but wrong accent mark
            int fullPoints = scoring.Calculate(true, difficulty, new AnswerTiming(answerSeconds, DefaultTimeLimitSeconds));
            pointsEarned = fullPoints / 2;
        }

        int totalPoints = session.RecordAnswer(request.PlayerId, isCorrect, pointsEarned, answerSeconds);

        return (
            new AccentuationAnswerChecked(
                status,
                isCorrect,
                question.CorrectPosition,
                question.CorrectAccentType,
                question.AccentedWord,
                question.Rule,
                pointsEarned,
                totalPoints
            ),
            AccentuationGameError.None
        );
    }

    public (AccentuationGameResult? Result, AccentuationGameError Error) GetResult(
        Guid sessionId,
        Guid playerId)
    {
        var session = sessions.Get(sessionId);
        if (session is null) return (null, AccentuationGameError.SessionNotFound);


        var stats = session.GetPlayerStats(playerId);
        if (stats is null)
            return (null, AccentuationGameError.PlayerNotFound);

        var (answered, correct, points, totalSeconds) = stats.Value;

        if (answered < session.QuestionCount)
            return (null, AccentuationGameError.RoundNotFinished);

        if (session.UserId is not null && session.TryMarkSaved(playerId))
        {
            try
            {
                int categoryId = statsTracker.GetOrCreateCategoryId(CategoryName);

                statsTracker.RecordGame(new AppGameResults
                {
                    UserId = session.UserId,
                    CategoryId = categoryId,
                    Difficulty = session.Levels.Max(GetDifficultyForLevel),
                    Score = points,
                    QuestionsAnswered = answered,
                    CorrectAnswers = correct,
                    TotalAnswerTimeSeconds = totalSeconds
                });
            }
            catch
            {
                session.UnmarkSaved(playerId);
                throw;
            }
        }

        double accuracy = Math.Round((double)correct / answered * 100, 1);

        AccentuationGameResult result = new(
            session.QuestionCount,
            correct,
            answered - correct,
            accuracy,
            points,
            session.UserId is not null
        );

        return (result, AccentuationGameError.None);
    }

    private static DifficultyLevel GetDifficultyForLevel(int level) => level switch
    {
        1 or 2 => DifficultyLevel.Easy,
        3 or 4 => DifficultyLevel.Medium,
        _ => DifficultyLevel.Hard
    };
}

// Helper classes for storing session and question state in memory
public class StoredAccentuationQuestion
{
    private readonly object sync = new();
    private readonly HashSet<Guid> answeredPlayers = new();

    public Guid SessionId { get; init; }
    public int Level { get; init; }
    public string Word { get; init; } = string.Empty;
    public string AccentedWord { get; init; } = string.Empty;
    public int CorrectPosition { get; init; }
    public string CorrectAccentType { get; init; } = string.Empty;
    public string Rule { get; init; } = string.Empty;
    public DateTime ServedAt { get; } = DateTime.UtcNow;

    public bool TryMarkAnswered(Guid playerId)
    {
        lock (sync)
        {
            return answeredPlayers.Add(playerId);
        }
    }
}

public class AccentuationSession
{
    private readonly object sync = new();
    private readonly HashSet<string> usedQuestions = new();
    private readonly Dictionary<Guid, PlayerProgress> players = new();
    private int generatedQuestions = 0;

    public int QuestionCount { get; }
    public IReadOnlyList<int> Levels { get; }
    public string? UserId { get; }
    private readonly Random _rng;

    public AccentuationSession(int questionCount, List<int> levels, string? userId, int? seed = null)
    {
        QuestionCount = questionCount;
        Levels = Array.AsReadOnly(levels.ToArray());
        UserId = userId;
        _rng = new Random(seed ?? Random.Shared.Next());
    }

    public bool TryRecordQuestion(string questionKey, out int questionNumber)
    {
        lock (sync)
        {
            if (generatedQuestions >= QuestionCount || !usedQuestions.Add(questionKey))
            {
                questionNumber = 0;
                return false;
            }

            generatedQuestions++;
            questionNumber = generatedQuestions;
            return true;
        }
    }

    public Guid AddPlayer()
    {
        lock (sync)
        {
            Guid playerId = Guid.NewGuid();
            players.Add(playerId, new PlayerProgress());
            return playerId;
        }
    }

    public bool HasPlayer(Guid playerId)
    {
        lock (sync)
        {
            return players.ContainsKey(playerId);
        }
    }

    public int RecordAnswer(Guid playerId, bool isCorrect, int points, double answerTimeSeconds)
    {
        lock (sync)
        {
            PlayerProgress progress = players[playerId];
            progress.RecordAnswer(isCorrect, points, answerTimeSeconds);
            return progress.Points;
        }
    }

    public PlayerStats? GetPlayerStats(Guid playerId)
    {
        lock (sync)
        {
            if (!players.TryGetValue(playerId, out var progress))
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
        lock (sync)
        {
            PlayerProgress progress = players[playerId];
            if (progress.Saved) return false;
            progress.Saved = true;
            return true;
        }
    }

    public void UnmarkSaved(Guid playerId)
    {
        lock (sync)
        {
            players[playerId].Saved = false;
        }
    }

    public bool IsFull
    {
        get
        {
            lock (sync)
                return generatedQuestions >= QuestionCount;
        }
    }

    public int NextRandom()
    {
        lock (sync)
        {
            return _rng.Next();
        }
    }
}
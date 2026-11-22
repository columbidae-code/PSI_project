using beveikZinojau.lt.Models;

namespace beveikZinojau.Tests.Geography;

public class StoredGeoQuestionTests
{
    [Fact]
    public void TryAnswer_CorrectAnswer_IsCorrect()
    {
        var question = new StoredGeoQuestion
        {
            CorrectOptionIndex = 2
        };

        Guid playerId = Guid.NewGuid();

        bool accepted = question.TryAnswer(
            playerId, 2, 30,
            out bool isCorrect,
            out double answerSeconds
        );

        Assert.True(accepted);
        Assert.True(isCorrect);
        Assert.InRange(answerSeconds, 0, 30);
    }

    [Fact]
    public void TryAnswer_WrongAnswer_IsIncorrect()
    {
        var question = new StoredGeoQuestion
        {
            CorrectOptionIndex = 2
        };

        bool accepted = question.TryAnswer(
            Guid.NewGuid(), 1, 30,
            out bool isCorrect,
            out _
        );

        Assert.True(accepted);
        Assert.False(isCorrect);
    }

    [Fact]
    public void TryAnswer_SkippedQuestion_IsIncorrect()
    {
        var question = new StoredGeoQuestion
        {
            CorrectOptionIndex = 2
        };

        bool accepted = question.TryAnswer(
            Guid.NewGuid(), null, 30,
            out bool isCorrect,
            out _
        );

        Assert.True(accepted);
        Assert.False(isCorrect);
    }

    [Fact]
    public void TryAnswer_ExpiredQuestion_IsIncorrect()
    {
        var question = new StoredGeoQuestion
        {
            CorrectOptionIndex = 2,
            ServedAt = DateTime.UtcNow.AddSeconds(-20)
        };

        bool accepted = question.TryAnswer(
            Guid.NewGuid(), 2, 10,
            out bool isCorrect,
            out double answerSeconds
        );

        Assert.True(accepted);
        Assert.False(isCorrect);
        Assert.Equal(10, answerSeconds);
    }

    [Fact]
    public void TryAnswer_SamePlayerTwice_RejectsSecondAnswer()
    {
        var question = new StoredGeoQuestion
        {
            CorrectOptionIndex = 2
        };

        Guid playerId = Guid.NewGuid();

        bool first = question.TryAnswer(
            playerId, 2, 30,
            out _, out _
        );

        bool second = question.TryAnswer(
            playerId, 1, 30,
            out _, out _
        );

        Assert.True(first);
        Assert.False(second);
    }
}
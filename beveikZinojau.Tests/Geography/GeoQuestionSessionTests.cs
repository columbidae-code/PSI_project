using beveikZinojau.lt.Models;

namespace beveikZinojau.Tests.Geography;

public class GeoQuestionSessionTests
{
    [Fact]
    public void TryRecordQuestion_DuplicateQuestion_ReturnsFalse()
    {
        var session = new GeoQuestionSession(
            5,
            new List<string> { "capital" },
            null
        );

        var question = new GeoQuestionKey("LT", "capital");

        bool first = session.TryRecordQuestion(question, out _);
        bool second = session.TryRecordQuestion(question, out _);

        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public void TryRecordQuestion_StopsAtQuestionLimit()
    {
        var session = new GeoQuestionSession(
            2,
            new List<string> { "capital" },
            null
        );

        bool first = session.TryRecordQuestion(
            new GeoQuestionKey("LT", "capital"), out int firstNumber);

        bool second = session.TryRecordQuestion(
            new GeoQuestionKey("LV", "capital"), out int secondNumber);

        bool third = session.TryRecordQuestion(
            new GeoQuestionKey("EE", "capital"), out _);

        Assert.True(first);
        Assert.True(second);
        Assert.False(third);

        Assert.Equal(1, firstNumber);
        Assert.Equal(2, secondNumber);
        Assert.True(session.IsFull);
    }

    [Fact]
    public void RecordAnswer_UpdatesPlayerStatistics()
    {
        var session = new GeoQuestionSession(
            5,
            new List<string> { "capital" },
            null
        );

        Guid playerId = session.AddPlayer();

        session.RecordAnswer(playerId, true, 100, 2.5);
        session.RecordAnswer(playerId, false, 0, 4.0);

        PlayerStats? stats = session.GetPlayerStats(playerId);

        Assert.True(stats.HasValue);
        Assert.Equal(2, stats.Value.Answered);
        Assert.Equal(1, stats.Value.Correct);
        Assert.Equal(100, stats.Value.Points);
        Assert.Equal(6.5, stats.Value.TotalAnswerTimeSeconds);
    }

    [Fact]
    public void TryMarkSaved_PreventsDuplicateSave()
    {
        var session = new GeoQuestionSession(
            5,
            new List<string> { "capital" },
            null
        );

        Guid playerId = session.AddPlayer();

        bool first = session.TryMarkSaved(playerId);
        bool second = session.TryMarkSaved(playerId);

        Assert.True(first);
        Assert.False(second);

        session.UnmarkSaved(playerId);

        Assert.True(session.TryMarkSaved(playerId));
    }
}

using System.Globalization;
using beveikZinojau.lt.Data;
using beveikZinojau.lt.Models;
using beveikZinojau.lt.Scoring;
using beveikZinojau.lt.Services.MathGame;
using beveikZinojau.lt.Services.MathGame.MathEnum;
using beveikZinojau.lt.Services.MathGame.MathRecords;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace beveikZinojau.Tests.MathTests
{
    public class MathGameServiceTests : IDisposable
    {
        private readonly SqliteConnection connection;
        private readonly AppDbContext db;
        private readonly MathGameService service;

        public MathGameServiceTests()
        {
            // The in-memory database only lives while this connection is open
            connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            db = new AppDbContext(options);
            db.Database.EnsureCreated();

            service = new MathGameService(
                new MemoryCache(new MemoryCacheOptions()),
                new MathQuestionGenerator(),
                new ScoringCalculator(new ScoringConfig()),
                new StatsTracker(db));
        }

        public void Dispose()
        {
            db.Dispose();
            connection.Dispose();
        }

        // helpers

        private AppUser AddUser(string name)
        {
            AppUser user = new AppUser { UserName = name };
            db.Users.Add(user);
            db.SaveChanges();
            return user;
        }

        private AppUserStats GetStats(string userId, int categoryId)
        {
            foreach (AppUserStats stats in db.UserStats.ToList())
            {
                if (stats.UserId == userId && stats.CategoryId == categoryId)
                {
                    return stats;
                }
            }
            throw new InvalidOperationException("Stats row not found.");
        }

        // Starts a session and returns its id and the id of the first player
        private (Guid SessionId, Guid PlayerId) StartSession(
            int questionCount, string questionType = "add", string? userId = null)
        {
            GeoSessionCreateRequest request = new GeoSessionCreateRequest
            {
                QuestionCount = questionCount,
                QuestionTypes = new List<string> { questionType }
            };

            var (session, _) = service.CreateSession(request, userId);

            Assert.NotNull(session);
            return (session.SessionId, session.PlayerId);
        }

        private MathQuestionIssued NextQuestion(Guid sessionId)
        {
            var (question, _) = service.GetQuestion(sessionId);

            Assert.NotNull(question);
            return question;
        }

        private static MathAnswerRequest MakeAnswer(MathQuestionIssued question, Guid playerId, string? answer)
        {
            return new MathAnswerRequest
            {
                QuestionId = question.QuestionId,
                PlayerId = playerId,
                Answer = answer
            };
        }

        private static string CorrectAnswerText(MathQuestionIssued question)
        {
            return Solve(question.Text).ToString(CultureInfo.InvariantCulture);
        }

        private void PlayRound(Guid sessionId, Guid playerId, int questionCount, bool answerCorrectly)
        {
            for (int i = 0; i < questionCount; i++)
            {
                MathQuestionIssued question = NextQuestion(sessionId);
                int solved = Solve(question.Text);
                int typed = answerCorrectly ? solved : solved + 1;

                service.CheckAnswer(
                    sessionId,
                    MakeAnswer(question, playerId, typed.ToString(CultureInfo.InvariantCulture)));
            }
        }

        private static int Solve(string text)
        {
            string[] parts = text.Split(' ');
            int first = int.Parse(parts[0]);
            int second = int.Parse(parts[2]);

            switch (parts[1])
            {
                case "+": return first + second;
                case "-": return first - second;
                case "×": return first * second;
                case "÷": return first / second;
                default: throw new InvalidOperationException("Unknown symbol: " + parts[1]);
            }
        }

        // CreateSession

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void CreateSession_QuestionCountNotPositive_ReturnsError(int count)
        {
            GeoSessionCreateRequest request = new GeoSessionCreateRequest
            {
                QuestionCount = count,
                QuestionTypes = new List<string> { "add" }
            };

            var (session, error) = service.CreateSession(request);

            Assert.Null(session);
            Assert.Equal("Question count must be positive.", error);
        }

        [Fact]
        public void CreateSession_NoTypes_ReturnsError()
        {
            GeoSessionCreateRequest request = new GeoSessionCreateRequest
            {
                QuestionCount = 5,
                QuestionTypes = new List<string>()
            };

            var (session, error) = service.CreateSession(request);

            Assert.Null(session);
            Assert.Equal("Select at least one operation.", error);
        }

        [Fact]
        public void CreateSession_OnlyBlankTypes_ReturnsError()
        {
            GeoSessionCreateRequest request = new GeoSessionCreateRequest
            {
                QuestionCount = 5,
                QuestionTypes = new List<string> { "", "   " }
            };

            var (session, error) = service.CreateSession(request);

            Assert.Null(session);
            Assert.Equal("Select at least one operation.", error);
        }

        [Fact]
        public void CreateSession_UnknownType_ReturnsError()
        {
            GeoSessionCreateRequest request = new GeoSessionCreateRequest
            {
                QuestionCount = 5,
                QuestionTypes = new List<string> { "add", "pow" }
            };

            var (session, error) = service.CreateSession(request);

            Assert.Null(session);
            Assert.Equal("Unknown operation selected.", error);
        }

        [Fact]
        public void CreateSession_ValidRequest_ReturnsSessionWithPlayer()
        {
            GeoSessionCreateRequest request = new GeoSessionCreateRequest
            {
                QuestionCount = 5,
                QuestionTypes = new List<string> { "add" }
            };

            var (session, error) = service.CreateSession(request);

            Assert.Null(error);
            Assert.NotNull(session);
            Assert.Equal(5, session.QuestionCount);
            Assert.NotEqual(Guid.Empty, session.SessionId);
            Assert.NotEqual(Guid.Empty, session.PlayerId);
        }

        [Fact]
        public void CreateSession_TypesWithSpacesAndCase_AreNormalizedAndDeduplicated()
        {
            GeoSessionCreateRequest request = new GeoSessionCreateRequest
            {
                QuestionCount = 5,
                QuestionTypes = new List<string> { " ADD ", "add", "Mul" }
            };

            var (session, _) = service.CreateSession(request);

            Assert.NotNull(session);
            Assert.Equal(2, session.QuestionTypes.Count);
            Assert.Equal("add", session.QuestionTypes[0]);
            Assert.Equal("mul", session.QuestionTypes[1]);
        }

        // GetQuestion

        [Fact]
        public void GetQuestion_UnknownSession_ReportsSessionMissing()
        {
            var (question, sessionMissing) = service.GetQuestion(Guid.NewGuid());

            Assert.Null(question);
            Assert.True(sessionMissing);
        }

        [Fact]
        public void GetQuestion_FirstQuestion_HasNumberOneAndRoundTotal()
        {
            var (sessionId, _) = StartSession(questionCount: 3);

            var (question, sessionMissing) = service.GetQuestion(sessionId);

            Assert.False(sessionMissing);
            Assert.NotNull(question);
            Assert.Equal(1, question.QuestionNumber);
            Assert.Equal(3, question.TotalQuestions);
            Assert.EndsWith(" = ?", question.Text);
        }

        [Fact]
        public void GetQuestion_SecondQuestion_HasNumberTwo()
        {
            var (sessionId, _) = StartSession(questionCount: 3);

            NextQuestion(sessionId);
            MathQuestionIssued second = NextQuestion(sessionId);

            Assert.Equal(2, second.QuestionNumber);
        }

        [Theory]
        [InlineData("add", 15.0)]
        [InlineData("sub", 15.0)]
        [InlineData("mul", 20.0)]
        [InlineData("div", 25.0)]
        public void GetQuestion_TimeLimit_MatchesTheRuleForTheType(string type, double expectedLimit)
        {
            var (sessionId, _) = StartSession(questionCount: 1, questionType: type);

            MathQuestionIssued question = NextQuestion(sessionId);

            Assert.Equal(expectedLimit, question.TimeLimitSeconds);
        }

        [Fact]
        public void GetQuestion_AfterRoundIsFull_ReturnsNullButSessionExists()
        {
            var (sessionId, _) = StartSession(questionCount: 2);
            NextQuestion(sessionId);
            NextQuestion(sessionId);

            var (extra, sessionMissing) = service.GetQuestion(sessionId);

            Assert.Null(extra);
            Assert.False(sessionMissing);
        }

        [Fact]
        public void GetQuestion_QuestionsInOneRound_AreAllDifferent()
        {
            var (sessionId, _) = StartSession(questionCount: 10);
            HashSet<string> texts = new HashSet<string>();

            for (int i = 0; i < 10; i++)
            {
                MathQuestionIssued question = NextQuestion(sessionId);
                texts.Add(question.Text);
            }

            Assert.Equal(10, texts.Count);
        }

        // CheckAnswer


        [Fact]
        public void CheckAnswer_UnknownSession_ReturnsSessionNotFound()
        {
            MathAnswerRequest request = new MathAnswerRequest
            {
                QuestionId = Guid.NewGuid(),
                PlayerId = Guid.NewGuid(),
                Answer = "1"
            };

            var (answer, error) = service.CheckAnswer(Guid.NewGuid(), request);

            Assert.Null(answer);
            Assert.Equal(MathGameError.SessionNotFound, error);
        }

        [Fact]
        public void CheckAnswer_UnknownPlayer_ReturnsPlayerNotFound()
        {
            var (sessionId, _) = StartSession(questionCount: 3);
            MathQuestionIssued question = NextQuestion(sessionId);

            var (answer, error) = service.CheckAnswer(
                sessionId, MakeAnswer(question, Guid.NewGuid(), "1"));

            Assert.Null(answer);
            Assert.Equal(MathGameError.PlayerNotFound, error);
        }

        [Fact]
        public void CheckAnswer_UnknownQuestion_ReturnsQuestionNotFound()
        {
            var (sessionId, playerId) = StartSession(questionCount: 3);
            MathAnswerRequest request = new MathAnswerRequest
            {
                QuestionId = Guid.NewGuid(),
                PlayerId = playerId,
                Answer = "1"
            };

            var (answer, error) = service.CheckAnswer(sessionId, request);

            Assert.Null(answer);
            Assert.Equal(MathGameError.QuestionNotFound, error);
        }

        [Fact]
        public void CheckAnswer_QuestionFromAnotherSession_ReturnsWrongSession()
        {
            var (sessionA, _) = StartSession(questionCount: 3);
            var (sessionB, playerB) = StartSession(questionCount: 3);
            MathQuestionIssued questionFromA = NextQuestion(sessionA);

            var (answer, error) = service.CheckAnswer(
                sessionB, MakeAnswer(questionFromA, playerB, "1"));

            Assert.Null(answer);
            Assert.Equal(MathGameError.WrongSession, error);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CheckAnswer_BlankAnswer_ReturnsMissingAnswer(string? typed)
        {
            var (sessionId, playerId) = StartSession(questionCount: 3);
            MathQuestionIssued question = NextQuestion(sessionId);

            var (answer, error) = service.CheckAnswer(
                sessionId, MakeAnswer(question, playerId, typed));

            Assert.Null(answer);
            Assert.Equal(MathGameError.MissingAnswer, error);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("1.5")]
        [InlineData("12abc")]
        public void CheckAnswer_NotAWholeNumber_ReturnsInvalidAnswer(string typed)
        {
            var (sessionId, playerId) = StartSession(questionCount: 3);
            MathQuestionIssued question = NextQuestion(sessionId);

            var (answer, error) = service.CheckAnswer(
                sessionId, MakeAnswer(question, playerId, typed));

            Assert.Null(answer);
            Assert.Equal(MathGameError.InvalidAnswer, error);
        }

        [Fact]
        public void CheckAnswer_SameQuestionTwice_ReturnsAlreadyAnswered()
        {
            var (sessionId, playerId) = StartSession(questionCount: 3);
            MathQuestionIssued question = NextQuestion(sessionId);
            MathAnswerRequest request = MakeAnswer(question, playerId, CorrectAnswerText(question));

            service.CheckAnswer(sessionId, request);
            var (answer, error) = service.CheckAnswer(sessionId, request);

            Assert.Null(answer);
            Assert.Equal(MathGameError.AlreadyAnswered, error);
        }

        [Fact]
        public void CheckAnswer_CorrectAnswer_GivesPoints()
        {
            var (sessionId, playerId) = StartSession(questionCount: 3);
            MathQuestionIssued question = NextQuestion(sessionId);

            var (answer, error) = service.CheckAnswer(
                sessionId, MakeAnswer(question, playerId, CorrectAnswerText(question)));

            Assert.Equal(MathGameError.None, error);
            Assert.NotNull(answer);
            Assert.True(answer.IsCorrect);
            Assert.Equal(Solve(question.Text), answer.CorrectAnswer);
            Assert.True(answer.PointsEarned > 0);
            Assert.Equal(answer.PointsEarned, answer.TotalPoints);
        }

        [Fact]
        public void CheckAnswer_WrongAnswer_GivesNoPointsAndRevealsCorrectAnswer()
        {
            var (sessionId, playerId) = StartSession(questionCount: 3);
            MathQuestionIssued question = NextQuestion(sessionId);
            int solved = Solve(question.Text);

            var (answer, error) = service.CheckAnswer(
                sessionId, MakeAnswer(question, playerId, (solved + 1).ToString(CultureInfo.InvariantCulture)));

            Assert.Equal(MathGameError.None, error);
            Assert.NotNull(answer);
            Assert.False(answer.IsCorrect);
            Assert.Equal(solved, answer.CorrectAnswer);
            Assert.Equal(0, answer.PointsEarned);
            Assert.Equal(0, answer.TotalPoints);
        }

        [Fact]
        public void CheckAnswer_SeveralCorrectAnswers_TotalPointsAccumulate()
        {
            var (sessionId, playerId) = StartSession(questionCount: 3);

            MathQuestionIssued firstQuestion = NextQuestion(sessionId);
            var (first, _) = service.CheckAnswer(
                sessionId, MakeAnswer(firstQuestion, playerId, CorrectAnswerText(firstQuestion)));

            MathQuestionIssued secondQuestion = NextQuestion(sessionId);
            var (second, _) = service.CheckAnswer(
                sessionId, MakeAnswer(secondQuestion, playerId, CorrectAnswerText(secondQuestion)));

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.Equal(first.PointsEarned + second.PointsEarned, second.TotalPoints);
        }

        [Fact]
        public void CheckAnswer_NegativeAnswerWithUnicodeMinus_IsAccepted()
        {
            // the game can acceps − sign
            var (sessionId, playerId) = StartSession(questionCount: 30, questionType: "sub");
            bool sawNegative = false;

            for (int i = 0; i < 30; i++)
            {
                MathQuestionIssued question = NextQuestion(sessionId);
                int solved = Solve(question.Text);
                string typed = solved.ToString(CultureInfo.InvariantCulture);

                if (solved < 0)
                {
                    sawNegative = true;
                    typed = "−" + Math.Abs(solved).ToString(CultureInfo.InvariantCulture);
                }

                var (answer, error) = service.CheckAnswer(
                    sessionId, MakeAnswer(question, playerId, typed));

                Assert.Equal(MathGameError.None, error);
                Assert.NotNull(answer);
                Assert.True(answer.IsCorrect);
            }

            Assert.True(sawNegative);
        }

        // GetResult
  

        [Fact]
        public void GetResult_UnknownSession_ReturnsSessionNotFound()
        {
            var (result, error) = service.GetResult(Guid.NewGuid(), Guid.NewGuid());

            Assert.Null(result);
            Assert.Equal(MathGameError.SessionNotFound, error);
        }

        [Fact]
        public void GetResult_UnknownPlayer_ReturnsPlayerNotFound()
        {
            var (sessionId, _) = StartSession(questionCount: 3);

            var (result, error) = service.GetResult(sessionId, Guid.NewGuid());

            Assert.Null(result);
            Assert.Equal(MathGameError.PlayerNotFound, error);
        }

        [Fact]
        public void GetResult_RoundNotFinished_ReturnsRoundNotFinished()
        {
            var (sessionId, playerId) = StartSession(questionCount: 3);
            PlayRound(sessionId, playerId, questionCount: 1, answerCorrectly: true);

            var (result, error) = service.GetResult(sessionId, playerId);

            Assert.Null(result);
            Assert.Equal(MathGameError.RoundNotFinished, error);
        }

        [Fact]
        public void GetResult_AllAnswersCorrect_ShowsFullScore()
        {
            var (sessionId, playerId) = StartSession(questionCount: 3);
            PlayRound(sessionId, playerId, questionCount: 3, answerCorrectly: true);

            var (result, error) = service.GetResult(sessionId, playerId);

            Assert.Equal(MathGameError.None, error);
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalQuestions);
            Assert.Equal(3, result.CorrectAnswers);
            Assert.Equal(0, result.WrongAnswers);
            Assert.Equal(100.0, result.AccuracyPercent);
            Assert.True(result.TotalPoints > 0);
        }

        [Fact]
        public void GetResult_AllAnswersWrong_ShowsZeroScore()
        {
            var (sessionId, playerId) = StartSession(questionCount: 3);
            PlayRound(sessionId, playerId, questionCount: 3, answerCorrectly: false);

            var (result, error) = service.GetResult(sessionId, playerId);

            Assert.Equal(MathGameError.None, error);
            Assert.NotNull(result);
            Assert.Equal(0, result.CorrectAnswers);
            Assert.Equal(3, result.WrongAnswers);
            Assert.Equal(0.0, result.AccuracyPercent);
            Assert.Equal(0, result.TotalPoints);
        }

        [Fact]
        public void GetResult_GuestPlayer_IsNotSavedToDatabase()
        {
            var (sessionId, playerId) = StartSession(questionCount: 3, userId: null);
            PlayRound(sessionId, playerId, questionCount: 3, answerCorrectly: true);

            var (result, _) = service.GetResult(sessionId, playerId);

            Assert.NotNull(result);
            Assert.False(result.Saved);
            Assert.Equal(0, db.GameResults.Count());
            Assert.Equal(0, db.UserStats.Count());
        }

        [Fact]
        public void GetResult_LoggedInUser_SavesGameAndStats()
        {
            AppUser user = AddUser("User1");
            var (sessionId, playerId) = StartSession(questionCount: 3, userId: user.Id);
            PlayRound(sessionId, playerId, questionCount: 3, answerCorrectly: true);

            var (result, error) = service.GetResult(sessionId, playerId);

            Assert.Equal(MathGameError.None, error);
            Assert.NotNull(result);
            Assert.True(result.Saved);
            Assert.Equal(1, db.GameResults.Count());
            Assert.Equal(1, db.Categories.Count());

            AppCategory category = db.Categories.First();
            Assert.Equal(MathQuestionRules.CategoryName, category.CategoryName);

            AppUserStats stats = GetStats(user.Id, category.Id);
            Assert.Equal(1, stats.GamesPlayed);
            Assert.Equal(1, stats.GamesWon);
            Assert.Equal(3, stats.TotalQuestions);
            Assert.Equal(3, stats.CorrectAnswers);
            Assert.Equal((long)result.TotalPoints, stats.TotalScore);
        }

        [Fact]
        public void GetResult_CalledTwice_SavesOnlyOnce()
        {
            AppUser user = AddUser("User1");
            var (sessionId, playerId) = StartSession(questionCount: 3, userId: user.Id);
            PlayRound(sessionId, playerId, questionCount: 3, answerCorrectly: true);

            service.GetResult(sessionId, playerId);
            service.GetResult(sessionId, playerId);

            Assert.Equal(1, db.GameResults.Count());
            Assert.Equal(1, db.UserStats.First().GamesPlayed);
        }

        [Theory]
        [InlineData("add", DifficultyLevel.Easy)]
        [InlineData("mul", DifficultyLevel.Medium)]
        [InlineData("div", DifficultyLevel.Hard)]
        public void GetResult_SavedGame_UsesDifficultyOfTheQuestionType(string type, DifficultyLevel expected)
        {
            AppUser user = AddUser("User1");
            var (sessionId, playerId) = StartSession(questionCount: 2, questionType: type, userId: user.Id);
            PlayRound(sessionId, playerId, questionCount: 2, answerCorrectly: true);

            service.GetResult(sessionId, playerId);

            AppGameResults saved = db.GameResults.First();
            Assert.Equal(expected, saved.Difficulty);
        }
    }
}

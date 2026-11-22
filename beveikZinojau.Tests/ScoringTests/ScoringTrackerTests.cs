using beveikZinojau.lt.Data;
using beveikZinojau.lt.Models;
using beveikZinojau.lt.Scoring;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace beveikZinojau.Tests.ScoringTests
{
    public class StatsTrackerTests : IDisposable
    {
        private readonly SqliteConnection connection;
        private readonly AppDbContext db;
        private readonly StatsTracker tracker;

        public StatsTrackerTests()
        {
            connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            db = new AppDbContext(options);
            db.Database.EnsureCreated();

            tracker = new StatsTracker(db);
        }

        public void Dispose()
        {
            db.Dispose();
            connection.Dispose();
        }

        //helpers

        
        private AppUser AddUser(string name)
        {
            AppUser user = new AppUser { UserName = name };
            db.Users.Add(user);
            db.SaveChanges();
            return user;
        }

        private static AppGameResults MakeResult(
            string userId, int categoryId, int score,
            int answered = 10, int correct = 7, double seconds = 20.0)
        {
            return new AppGameResults
            {
                UserId = userId,
                CategoryId = categoryId,
                Difficulty = DifficultyLevel.Easy,
                Score = score,
                QuestionsAnswered = answered,
                CorrectAnswers = correct,
                TotalAnswerTimeSeconds = seconds
            };
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

        //GetOrCreateCategoryId

        [Fact]
        public void GetOrCreateCategoryId_NewCategory_CreatesCategory()
        {
            int id = tracker.GetOrCreateCategoryId("Math");

            AppCategory? saved = db.Categories.Find(id);
            Assert.NotNull(saved);
            Assert.Equal("Math", saved.CategoryName);
        }

        [Fact]
        public void GetOrCreateCategoryId_ExistingCategory_ReturnsSameIdWithoutDuplicate()
        {
            int first = tracker.GetOrCreateCategoryId("Math");
            int second = tracker.GetOrCreateCategoryId("Math");

            Assert.Equal(first, second);
            Assert.Equal(1, db.Categories.Count());
        }

        [Fact]
        public void GetOrCreateCategoryId_DifferentNames_ReturnDifferentIds()
        {
            int math = tracker.GetOrCreateCategoryId("Math");
            int history = tracker.GetOrCreateCategoryId("History");

            Assert.NotEqual(math, history);
            Assert.Equal(2, db.Categories.Count());
        }

        //RecordGame

        [Fact]
        public void RecordGame_FirstGame_SavesResultAndCreatesStats()
        {
            AppUser user = AddUser("User1");
            int categoryId = tracker.GetOrCreateCategoryId("Math");

            tracker.RecordGame(MakeResult(user.Id, categoryId, score: 80, answered: 10, correct: 7, seconds: 25.5));

            Assert.Equal(1, db.GameResults.Count());
            Assert.Equal(1, db.UserStats.Count());

            AppUserStats stats = GetStats(user.Id, categoryId);
            Assert.Equal(80L, stats.TotalScore);
            Assert.Equal(1, stats.GamesPlayed);
            Assert.Equal(1, stats.GamesWon);
            Assert.Equal(0, stats.GamesLost);
            Assert.Equal(10, stats.TotalQuestions);
            Assert.Equal(7, stats.CorrectAnswers);
            Assert.Equal(25.5, stats.TotalAnswerTimeSeconds);
        }

        [Fact]
        public void RecordGame_SecondGameSameCategory_AddsToExistingTotals()
        {
            AppUser user = AddUser("User1");
            int categoryId = tracker.GetOrCreateCategoryId("Math");

            tracker.RecordGame(MakeResult(user.Id, categoryId, score: 80, answered: 10, correct: 7, seconds: 25.5));
            tracker.RecordGame(MakeResult(user.Id, categoryId, score: 40, answered: 5, correct: 3, seconds: 10.0));

            Assert.Equal(2, db.GameResults.Count());
            Assert.Equal(1, db.UserStats.Count());

            AppUserStats stats = GetStats(user.Id, categoryId);
            Assert.Equal(120L, stats.TotalScore);
            Assert.Equal(2, stats.GamesPlayed);
            Assert.Equal(2, stats.GamesWon);
            Assert.Equal(15, stats.TotalQuestions);
            Assert.Equal(10, stats.CorrectAnswers);
            Assert.Equal(35.5, stats.TotalAnswerTimeSeconds);
        }

        [Theory]
        [InlineData(100, 1, 0)]
        [InlineData(1, 1, 0)]
        [InlineData(0, 0, 1)]
        [InlineData(-5, 0, 1)]
        public void RecordGame_Score_DecidesWinOrLoss(int score, int expectedWins, int expectedLosses)
        {
            AppUser user = AddUser("User1");
            int categoryId = tracker.GetOrCreateCategoryId("Math");

            tracker.RecordGame(MakeResult(user.Id, categoryId, score));

            AppUserStats stats = GetStats(user.Id, categoryId);
            Assert.Equal(expectedWins, stats.GamesWon);
            Assert.Equal(expectedLosses, stats.GamesLost);
        }

        [Fact]
        public void RecordGame_DifferentCategories_CreatesSeparateStatsRows()
        {
            AppUser user = AddUser("User1");
            int math = tracker.GetOrCreateCategoryId("Math");
            int history = tracker.GetOrCreateCategoryId("History");

            tracker.RecordGame(MakeResult(user.Id, math, score: 50));
            tracker.RecordGame(MakeResult(user.Id, history, score: 30));

            Assert.Equal(2, db.UserStats.Count());
            Assert.Equal(50L, GetStats(user.Id, math).TotalScore);
            Assert.Equal(30L, GetStats(user.Id, history).TotalScore);
        }

        [Fact]
        public void RecordGame_DifferentUsers_CreatesSeparateStatsRows()
        {
            AppUser first = AddUser("User1");
            AppUser second = AddUser("User2");
            int categoryId = tracker.GetOrCreateCategoryId("Math");

            tracker.RecordGame(MakeResult(first.Id, categoryId, score: 50));
            tracker.RecordGame(MakeResult(second.Id, categoryId, score: 70));

            Assert.Equal(2, db.UserStats.Count());
            Assert.Equal(50L, GetStats(first.Id, categoryId).TotalScore);
            Assert.Equal(70L, GetStats(second.Id, categoryId).TotalScore);
        }
    }
}
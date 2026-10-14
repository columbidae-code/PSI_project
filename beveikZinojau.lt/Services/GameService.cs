using beveikZinojau.lt.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace beveikZinojau.lt.Services
{
    public class GameService
    {
        private readonly AppDbContext _db;
        public GameService(AppDbContext db)
        {
            _db = db;
        }

        public class LeaderBoardEntry
        {
            public string username { get; set; }
            public int score { get; set; }
        }
        public class StatisticsEntry
        {
            public string category {  get; set; }
            public long    totalScore {  get; set; }
            public int    gamesPlayed { get; set; }
            public int    gamesWon    { get;  set; } 
            public int gamesLost { get; set; }
            public int totalQuestions  { get; set; }
            public int correctAnswers { get; set; }
            public double totalAnswerTimeSeconds { get; set; }
        }


        public List<LeaderBoardEntry> GetTopNUsers(int n)
        {

           var result = _db.GameResults
                 .GroupBy(r => r.User.UserName)
                 .Select(g => new LeaderBoardEntry { username = g.Key, score = g.Sum(r => r.Score) })
                 .OrderByDescending( x => x.score)
                 .Take(n)
                 .ToList();

            return result;
        }

        public List<StatisticsEntry> GetUserStats(string username)
        {
            var stats =  _db.UserStats
               .Where(s => s.User.UserName == username)
               .Select(s => new StatisticsEntry
               {
                   category = s.Category.CategoryName,
                   totalScore = s.TotalScore,
                   gamesPlayed = s.GamesPlayed,
                   gamesWon = s.GamesWon,
                   gamesLost = s.GamesLost,
                   totalQuestions = s.TotalQuestions,
                   correctAnswers = s.CorrectAnswers,
                   totalAnswerTimeSeconds = s.TotalAnswerTimeSeconds
               })
               .ToList();

            return stats;
        }
    }
}

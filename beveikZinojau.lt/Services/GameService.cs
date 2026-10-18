using beveikZinojau.lt.Data;
using beveikZinojau.lt.Services.ServiceHelpers;
using Microsoft.AspNetCore.Mvc;
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


        public List<LeaderBoardEntry> GetTopNUsers(int n)
        {
            return _db.GameResults
                .GroupBy(r => r.User.UserName)
                .OrderByDescending(g => g.Sum(r => r.Score))
                .Take(n)
                .Select(g => new LeaderBoardEntry(g.Key, g.Sum(r => r.Score)))
                .ToList();
        }

        public List<StatisticsEntry> GetUserStats(string username)
        {
            return _db.UserStats
                .Where(s => s.User.UserName == username)
                .Select(s => new StatisticsEntry(
                    Category: s.Category.CategoryName,
                    TotalScore: s.TotalScore,
                    GamesPlayed: s.GamesPlayed,
                    GamesWon: s.GamesWon,
                    GamesLost: s.GamesLost,
                    TotalQuestions: s.TotalQuestions,
                    CorrectAnswers: s.CorrectAnswers,
                    TotalAnswerTimeSeconds: s.TotalAnswerTimeSeconds))
                .ToList();
        }

        public PlayerSummary GetUserSummary(string username)
        {
            List<AppUserStats> stats = _db.UserStats
                .Where(s => s.User.UserName == username)
                .ToList();

            return PlayerSummary.FromStats(stats);
        }
    }
}

using beveikZinojau.lt.Data;
using beveikZinojau.lt.Services.ServiceHelpers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace beveikZinojau.lt.Services
{
    public class GameResultService
    {
        private readonly AppDbContext _db;
        public GameResultService(AppDbContext db)
        {
            _db = db;
        }


        public async Task<List<LeaderBoardEntry>> GetTopNUsersAsync(int NumberOfUsers)
        {
            return await _db.GameResults
                .GroupBy(r => r.User.UserName)
                .OrderByDescending(g => g.Sum(r => r.Score))
                .Take(NumberOfUsers)
                .Select(g => new LeaderBoardEntry(g.Key, g.Sum(r => r.Score)))
                .ToListAsync();
        }

        public async Task<List<StatisticsEntry>> GetUserStatsAsync(string username)
        {
            return await _db.UserStats
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
                .ToListAsync();
        }

        public async Task<PlayerSummary> GetUserSummaryAsync(string username)
        {
            List<AppUserStats> stats = await _db.UserStats
                .Where(s => s.User.UserName == username)
                .ToListAsync();

            return PlayerSummary.FromStats(stats);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using beveikZinojau.lt.Data;
using Microsoft.EntityFrameworkCore;

namespace beveikZinojau.lt.Scoring
{
    public class StatsTracker
    {
        private readonly AppDbContext _context;
        public StatsTracker(AppDbContext context)
        {
            _context = context;
        }
        // Finds a category by name and creates it if it does not exist yet
        public async Task<int> GetOrCreateCategoryIdAsync(string categoryName)
        {
            AppCategory? category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryName == categoryName);

            if (category == null)
            {
                category = new AppCategory { CategoryName = categoryName };
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
            }

            return category.Id;
        }
        public async Task RecordGameAsync(AppGameResults results)
        {
            _context.GameResults.Add(results);
            var userStats = await _context.UserStats.FirstOrDefaultAsync(s => s.UserId == results.UserId && s.CategoryId == results.CategoryId);
            if (userStats == null)
            {
                userStats = new AppUserStats
                {
                    UserId = results.UserId,
                    CategoryId = results.CategoryId
                };
                _context.UserStats.Add(userStats);
            }

            userStats.TotalScore += results.Score;
            userStats.GamesPlayed += 1;
            userStats.GamesWon += results.Score > 0 ? 1 : 0;
            userStats.GamesLost += results.Score <= 0 ? 1 : 0;
            userStats.TotalQuestions += results.QuestionsAnswered;
            userStats.CorrectAnswers += results.CorrectAnswers;
            userStats.TotalAnswerTimeSeconds += results.TotalAnswerTimeSeconds;


            await _context.SaveChangesAsync();
        }
    }
}

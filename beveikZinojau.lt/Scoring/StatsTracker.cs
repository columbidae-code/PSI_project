using System;
using System.Collections.Generic;
using System.Text;
using beveikZinojau.lt.Data;

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
        public int GetOrCreateCategoryId(string categoryName)
        {
            AppCategory? category = _context.Categories
                .FirstOrDefault(c => c.CategoryName == categoryName);

            if (category == null)
            {
                category = new AppCategory { CategoryName = categoryName };
                _context.Categories.Add(category);
                _context.SaveChanges();
            }

            return category.Id;
        }
public void RecordGame (AppGameResults results)
        {
            _context.GameResults.Add(results);
            var userStats = _context.UserStats.FirstOrDefault(s => s.UserId == results.UserId && s.CategoryId == results.CategoryId);
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

      
            _context.SaveChanges();
        }
    }
}

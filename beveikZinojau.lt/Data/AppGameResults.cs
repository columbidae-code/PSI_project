using System;
using System.Collections.Generic;
using System.Text;
using beveikZinojau.lt.Models;

namespace beveikZinojau.lt.Data
{
    public class AppGameResults
    {
        public int Id { get; set; }
        public string UserId { get; set; } = default!;
        public AppUser User { get; set; } = default!;
        public int CategoryId { get; set; }
        public AppCategory Category { get; set; } = default!;
        public DifficultyLevel Difficulty { get; set; }
        public int Score { get; set; }
        public int QuestionsAnswered { get; set; }
        public int CorrectAnswers { get; set; }
        public double TotalAnswerTimeSeconds { get; set; }
        public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
    }
}

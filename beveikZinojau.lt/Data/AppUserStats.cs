using System;
using System.Collections.Generic;
using System.Text;

namespace beveikZinojau.lt.Data
{
    public class AppUserStats
    {
        public int Id { get; set; }
        public string UserId { get; set; } = default!;
        public AppUser User { get; set; } = default!;
        public int CategoryId { get; set; }
        public AppCategory Category { get; set; } = default!;

        public long TotalScore { get; set; }
        public int GamesPlayed { get; set; }
        public int GamesWon { get; set; }    
        public int GamesLost { get; set; }        
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public double TotalAnswerTimeSeconds { get; set; }

        public uint RowVersion { get; set; }            // concurrency token

    }
}

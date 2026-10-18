using System;
using System.Collections.Generic;
using System.Text;

namespace beveikZinojau.lt.Services.ServiceHelpers
{
    public class StatisticsEntry
    {
        public string category { get; set; }
        public long totalScore { get; set; }
        public int gamesPlayed { get; set; }
        public int gamesWon { get; set; }
        public int gamesLost { get; set; }
        public int totalQuestions { get; set; }
        public int correctAnswers { get; set; }
        public double totalAnswerTimeSeconds { get; set; }
    }
}

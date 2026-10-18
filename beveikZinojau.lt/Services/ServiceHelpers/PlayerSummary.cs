using System;
using System.Collections.Generic;
using System.Text;
using beveikZinojau.lt.Data;

namespace beveikZinojau.lt.Services.ServiceHelpers
{

    public readonly record struct PlayerSummary(
        long TotalScore,
        int GamesPlayed,
        int GamesWon,
        int TotalQuestions,
        int CorrectAnswers,
        double TotalAnswerTimeSeconds)
    {
        public double WinRate => Divide(GamesWon, GamesPlayed);

        public double Accuracy => Divide(CorrectAnswers, TotalQuestions);

        public double AverageAnswerTimeSeconds => Divide(TotalAnswerTimeSeconds, TotalQuestions);

        public static PlayerSummary FromStats(IReadOnlyCollection<AppUserStats> stats) => new(
            stats.Sum(s => s.TotalScore),
            stats.Sum(s => s.GamesPlayed),
            stats.Sum(s => s.GamesWon),
            stats.Sum(s => s.TotalQuestions),
            stats.Sum(s => s.CorrectAnswers),
            stats.Sum(s => s.TotalAnswerTimeSeconds));

        private static double Divide(double top, double bottom) => bottom == 0 ? 0 : top / bottom;
    }
}

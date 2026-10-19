using System;
using System.Collections.Generic;
using System.Text;

namespace beveikZinojau.lt.Services.ServiceHelpers
{
    public record StatisticsEntry(string Category, long TotalScore, int GamesPlayed, int GamesWon, int GamesLost, int TotalQuestions, int CorrectAnswers, double TotalAnswerTimeSeconds);

}

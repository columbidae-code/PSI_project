using System;
using System.Collections.Generic;
using System.Text;
using beveikZinojau.lt.Models;
using Microsoft.Extensions.Options;

namespace beveikZinojau.lt.Scoring
{
    public class ScoringCalculator
    {
        private readonly ScoringConfig _config;

        public ScoringCalculator(IOptions<ScoringConfig> config)
        {
            _config = config.Value;
        }

        public int Calculate(bool isCorrect, DifficultyLevel difficulty, double timeTaken, double timeLimit)
        {
            if (!isCorrect || timeLimit <= 0 || timeTaken > timeLimit)
            {
                return 0;
            }

            double timeRatio = timeTaken/ timeLimit;
            double speedMultiplier = 1 - (1 - _config.CorrectAnswerShare) * timeRatio;

            double points = _config.BasePoints * speedMultiplier * GetDifficultyMultiplier(difficulty);

            return (int)Math.Round(points);
        }

        public double GetDifficultyMultiplier(DifficultyLevel difficulty)
        {
            return difficulty switch
            {
                DifficultyLevel.Easy => _config.EasyMultiplier,
                DifficultyLevel.Medium => _config.MediumMultiplier,
                DifficultyLevel.Hard => _config.HardMultiplier,
                _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null)
            };
        }
    }
}

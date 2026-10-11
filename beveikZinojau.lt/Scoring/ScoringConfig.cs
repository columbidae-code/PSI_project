using System;
using System.Collections.Generic;
using System.Text;

namespace beveikZinojau.lt.Scoring
{
    public class ScoringConfig
    {
        public int BasePoints { get; set; } = 100;

        public double CorrectAnswerShare { get; set; } = 0.5;

        public double EasyMultiplier { get; set; } = 1.0;
        public double MediumMultiplier { get; set; } = 1.5;
        public double HardMultiplier { get; set; } = 2.0;



    }
}

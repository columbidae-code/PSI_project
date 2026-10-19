using System;
using System.Collections.Generic;
using System.Text;

namespace beveikZinojau.lt.Scoring
{
    public readonly struct AnswerTiming
    {
        public AnswerTiming(double timeTaken, double timeLimit)
        {
            TimeTaken = timeTaken;
            TimeLimit = timeLimit;
        }

        public double TimeTaken { get; }
        public double TimeLimit { get; }

        public bool IsWithinLimit => TimeLimit > 0 && TimeTaken <= TimeLimit;

        public double Ratio => TimeLimit > 0 ? TimeTaken / TimeLimit : 0;
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace beveikZinojau.lt.Services.History.HistoryGameEnum
{
    public enum HistoryGameError
    {
        None,
        SessionNotFound,
        PlayerNotFound,
        QuestionNotFound,
        WrongSession,
        InvalidSubmission,
        AlreadyAnswered,
        RoundNotFinished
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace beveikZinojau.lt.Services.Accentuation.AccentuationGameEnum
{
    public enum AccentuationGameError
    {
        None,
        SessionNotFound,
        PlayerNotFound,
        QuestionNotFound,
        WrongSession,
        MissingAnswer,
        InvalidOption,
        AlreadyAnswered,
        RoundNotFinished
    }
}

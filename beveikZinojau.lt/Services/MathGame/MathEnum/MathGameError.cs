using System;
using System.Collections.Generic;
using System.Text;

namespace beveikZinojau.lt.Services.MathGame.MathEnum
{
    public enum MathGameError
    {
        None,
        SessionNotFound,
        PlayerNotFound,
        QuestionNotFound,
        WrongSession,
        MissingAnswer,
        InvalidAnswer,
        AlreadyAnswered,
        RoundNotFinished
    }


}

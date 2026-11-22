using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace beveikZinojau.lt.Services.ServiceHelpers
{
    public static class GameErrorMapper
    {
        public static IActionResult? ToResult<T>(T error) where T : struct, Enum
        {
            return error.ToString() switch
            {
                "SessionNotFound" => new NotFoundObjectResult("Session not found."),
                "PlayerNotFound" => new NotFoundObjectResult("Player not found."),
                "QuestionNotFound" => new NotFoundObjectResult("Question not found."),
                "WrongSession" => new BadRequestObjectResult("Question belongs to another session."),
                "AlreadyAnswered" => new ConflictObjectResult("Player already answered this question."),
                "RoundNotFinished" => new ConflictObjectResult("Round not finished."),
                _ => null
            };
        }
    }
}

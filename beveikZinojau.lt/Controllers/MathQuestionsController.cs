using beveikZinojau.lt.Data;
using beveikZinojau.lt.Models;
using beveikZinojau.lt.Scoring;
using beveikZinojau.lt.Services.Geography;
using beveikZinojau.lt.Services.MathGame;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Data;
using System.Security.Claims;
using System.Text.Json;

namespace beveikZinojau.lt.Controllers
{
    [ApiController, Route("api/mathquestions")]
    public class MathQuestionsController : ControllerBase
    {
        private readonly MathGameService gameService;

        public MathQuestionsController( MathGameService gameService)
        {
            this.gameService = gameService;
        }


        [HttpPost("session")]

        public IActionResult StartSession(
    [FromBody] GeoSessionCreateRequest request)
        {

            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var (session, error) = gameService.CreateSession(request, userId);

            if (session is null)
                return BadRequest(error);

            return Ok(session);
        }


        [HttpPost("session/{sessionId:guid}/questions")]
        public IActionResult GetQuestion(Guid sessionId)
        {
            var (question, sessionMissing) = gameService.GetQuestion(sessionId);

            if (sessionMissing)
                return NotFound("Session not found or expired.");

            if (question is null)
                return Conflict("Round finished or no unused questions remain.");

            return Ok(question);
        }


        [HttpPost("session/{sessionId:guid}/answer")]

        public IActionResult CheckAnswer(
    Guid sessionId,
    [FromBody] MathAnswerRequest request)
        {
            var (answer, error) = gameService.CheckAnswer(sessionId, request);

            return error switch
            {
                MathGameError.SessionNotFound =>
                    NotFound("Session not found."),

                MathGameError.PlayerNotFound =>
                    NotFound("Player not found."),

                MathGameError.QuestionNotFound =>
                    NotFound("Question not found."),

                MathGameError.WrongSession =>
                    BadRequest("Question belongs to another session."),

                MathGameError.MissingAnswer =>
                    BadRequest("Select an answer."),

                MathGameError.InvalidAnswer =>
                    BadRequest("Invalid Answer."),

                MathGameError.AlreadyAnswered =>
                    Conflict("Player already answered this question."),

                _ => Ok(answer)
            };
        }


        [HttpGet("session/{sessionId:guid}/result")]
        public IActionResult GetResult(
    Guid sessionId,
    [FromQuery] Guid playerId)
        {
            var (result, error) = gameService.GetResult(sessionId, playerId);

            return error switch
            {
                MathGameError.SessionNotFound =>
                    NotFound("Session not found."),

                MathGameError.PlayerNotFound =>
                    NotFound("Player not found."),

                MathGameError.RoundNotFinished =>
                    Conflict("Round not finished."),

                _ => Ok(result)
            };
        }

    }

}

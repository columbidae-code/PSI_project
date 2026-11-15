using beveikZinojau.lt.Models;
using beveikZinojau.lt.Services.History;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace beveikZinojau.lt.Controllers
{
    [ApiController, Route("api/historyquestions")]
    public class HistoryQuestionsController : ControllerBase
    {
        private readonly IMemoryCache cache;
        private readonly HistoryGameService gameService;

        public HistoryQuestionsController(IMemoryCache cache, HistoryGameService gameService)
        {
            this.cache = cache;
            this.gameService = gameService;
        }

        [HttpPost("session")]
        public IActionResult StartSession([FromBody] HistorySessionCreateRequest request)
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
            [FromBody] HistoryAnswerRequest request)
        {
            var (answer, error) = gameService.CheckAnswer(sessionId, request);

            return error switch
            {
                HistoryGameError.SessionNotFound =>
                    NotFound("Session not found."),

                HistoryGameError.PlayerNotFound =>
                    NotFound("Player not found."),

                HistoryGameError.QuestionNotFound =>
                    NotFound("Question not found."),

                HistoryGameError.WrongSession =>
                    BadRequest("Question belongs to another session."),

                HistoryGameError.InvalidSubmission =>
                    BadRequest("Invalid submission or incorrect number of event cards provided."),

                HistoryGameError.AlreadyAnswered =>
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
                HistoryGameError.SessionNotFound =>
                    NotFound("Session not found."),

                HistoryGameError.PlayerNotFound =>
                    NotFound("Player not found."),

                HistoryGameError.RoundNotFinished =>
                    Conflict("Round not finished."),

                _ => Ok(result)
            };
        }
    }
}
using beveikZinojau.lt.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using beveikZinojau.lt.Services.Geography;

namespace beveikZinojau.lt.Controllers
{
    [ApiController]
    [Route("api/geoquestions")]
    public class GeoQuestionsController : ControllerBase
    {
        private readonly IMemoryCache cache;
        private readonly GeoGameService gameService;

        public GeoQuestionsController(IMemoryCache cache, GeoGameService gameService)
        {
            this.cache = cache;
            this.gameService = gameService;
        }

        [HttpPost("session")]
        public IActionResult StartSession(
    [FromBody] GeoSessionCreateRequest request)
        {
            var (session, error) = gameService.CreateSession(request);

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
    [FromBody] GeoAnswerRequest request)
        {
            var (answer, error) = gameService.CheckAnswer(sessionId, request);

            return error switch
            {
                GeoGameError.SessionNotFound =>
                    NotFound("Session not found."),

                GeoGameError.PlayerNotFound =>
                    NotFound("Player not found."),

                GeoGameError.QuestionNotFound =>
                    NotFound("Question not found."),

                GeoGameError.WrongSession =>
                    BadRequest("Question belongs to another session."),

                GeoGameError.MissingAnswer =>
                    BadRequest("Select an answer."),

                GeoGameError.InvalidOption =>
                    BadRequest("Invalid option."),

                GeoGameError.AlreadyAnswered =>
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
                GeoGameError.SessionNotFound =>
                    NotFound("Session not found."),

                GeoGameError.PlayerNotFound =>
                    NotFound("Player not found."),

                GeoGameError.RoundNotFinished =>
                    Conflict("Round not finished."),

                _ => Ok(result)
            };
        }
    }
}

using beveikZinojau.lt.Data;
using beveikZinojau.lt.Models;
using beveikZinojau.lt.Scoring;
using beveikZinojau.lt.Services.Geography;
using beveikZinojau.lt.Services.ServiceHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Data;
using System.Security.Claims;
using System.Text.Json;

namespace beveikZinojau.lt.Controllers
{
    [ApiController, Route("api/geoquestions")]
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
    [FromBody] GeoAnswerRequest request)
        {
            var (answer, error) = gameService.CheckAnswer(sessionId, request);

            IActionResult? mapped = error switch
            {
                GeoGameError.MissingAnswer => BadRequest("Select an answer."),
                GeoGameError.InvalidOption => BadRequest("Invalid option."),
                _ => GameErrorMapper.ToResult(error)
            };

            return mapped ?? Ok(answer);
        }


        [HttpGet("session/{sessionId:guid}/result")]
        public IActionResult GetResult(
    Guid sessionId,
    [FromQuery] Guid playerId)
        {
            var (result, error) = gameService.GetResult(sessionId, playerId);

            return GameErrorMapper.ToResult(error) ?? Ok(result);
        }
    }
}

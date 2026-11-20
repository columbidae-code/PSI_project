using beveikZinojau.lt.Models;
using beveikZinojau.lt.Services.Geography;
using beveikZinojau.lt.Services.Geography.GeoGameEnum;
using beveikZinojau.lt.Services.Geography.GeoGameRecords;

using beveikZinojau.lt.Services.ServiceHelpers;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace beveikZinojau.lt.Controllers
{
    [ApiController, Route("api/geoquestions")]
    public class GeoQuestionsController : ControllerBase
    {
        private readonly GeoSessionService sessionService;
        private readonly GeoQuestionService questionService;
        private readonly GeoAnswerService answerService;
        private readonly GeoResultService resultService;

        public GeoQuestionsController(
            GeoSessionService sessionService,
            GeoQuestionService questionService,
            GeoAnswerService answerService,
            GeoResultService resultService)
        {
            this.sessionService = sessionService;
            this.questionService = questionService;
            this.answerService = answerService;
            this.resultService = resultService;
        }

        [HttpPost("session")]
        public IActionResult StartSession(
    [FromBody] GeoSessionCreateRequest request)
        {

            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var (session, error) = sessionService.CreateSession(request, userId);

            if (session is null)
                return BadRequest(error);

            return Ok(session);
        }

        [HttpPost("session/{sessionId:guid}/questions")]
        public IActionResult GetQuestion(Guid sessionId)
        {
            var (question, sessionMissing) = questionService.GetQuestion(sessionId);

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
            var (answer, error) = answerService.CheckAnswer(sessionId, request);

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
            var (result, error) = resultService.GetResult(sessionId, playerId);

            return GameErrorMapper.ToResult(error) ?? Ok(result);
        }
    }
}

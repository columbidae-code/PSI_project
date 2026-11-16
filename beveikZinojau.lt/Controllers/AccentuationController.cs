using beveikZinojau.lt.Services.Accentuation;
using beveikZinojau.lt.Services.ServiceHelpers;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace beveikZinojau.lt.Controllers;

[ApiController, Route("api/accentuation")]
public class AccentuationController : ControllerBase
{
    private readonly AccentuationGameService gameService;

    public AccentuationController(AccentuationGameService gameService)
    {
        this.gameService = gameService;
    }

    [HttpPost("session")]
    public IActionResult StartSession([FromBody] AccentuationSessionCreateRequest request)
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
    public IActionResult CheckAnswer(Guid sessionId, [FromBody] AccentuationAnswerRequest request)
    {
        var (answer, error) = gameService.CheckAnswer(sessionId, request);

        IActionResult? mapped = error switch
        {
            AccentuationGameError.MissingAnswer => BadRequest("Select a letter and accent mark."),
            AccentuationGameError.InvalidOption => BadRequest("Invalid letter position or accent type."),
            _ => GameErrorMapper.ToResult(error)
        };

        return mapped ?? Ok(answer);
    }

    [HttpGet("session/{sessionId:guid}/result")]
    public IActionResult GetResult(Guid sessionId, [FromQuery] Guid playerId)
    {
        var (result, error) = gameService.GetResult(sessionId, playerId);

        return GameErrorMapper.ToResult(error) ?? Ok(result);
    }
}
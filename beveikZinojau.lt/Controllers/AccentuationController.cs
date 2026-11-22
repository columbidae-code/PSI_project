using beveikZinojau.lt.Services.Accentuation;
using beveikZinojau.lt.Services.Accentuation.AccentuationGameEnum;
using beveikZinojau.lt.Services.Accentuation.AccentuationGameRecords;
using beveikZinojau.lt.Services.ServiceHelpers;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace beveikZinojau.lt.Controllers;

[ApiController, Route("api/accentuation")]
public class AccentuationController : ControllerBase
{
    private readonly AccentuationSessionService _sessionService;
    private readonly AccentuationQuestionService _questionService;
    private readonly AccentuationAnswerService _answerService;
    private readonly AccentuationResultService _resultService;

    public AccentuationController(
        AccentuationSessionService sessionService,
        AccentuationQuestionService questionService,
        AccentuationAnswerService answerService,
        AccentuationResultService resultService)
    {
        _sessionService = sessionService;
        _questionService = questionService;
        _answerService = answerService;
        _resultService = resultService;
    }

    [HttpPost("session")]
    public IActionResult StartSession([FromBody] AccentuationSessionCreateRequest request)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var (session, error) = _sessionService.CreateSession(request, userId);

        if (session is null)
            return BadRequest(error);

        return Ok(session);
    }

    [HttpPost("session/{sessionId:guid}/questions")]
    public async Task<IActionResult> GetQuestion(Guid sessionId)
    {
        var (question, sessionMissing) = await _questionService.GetQuestionAsync(
            sessionId,
            HttpContext.RequestAborted);

        if (sessionMissing)
            return NotFound("Session not found or expired.");

        if (question is null)
            return Conflict("Round finished or no unused questions remain.");

        return Ok(question);
    }

    [HttpPost("session/{sessionId:guid}/answer")]
    public IActionResult CheckAnswer(Guid sessionId, [FromBody] AccentuationAnswerRequest request)
    {
        var (answer, error) = _answerService.CheckAnswer(sessionId, request);

        IActionResult? mapped = error switch
        {
            AccentuationGameError.MissingAnswer => BadRequest("Select a letter and accent mark."),
            AccentuationGameError.InvalidOption => BadRequest("Invalid letter position or accent type."),
            _ => GameErrorMapper.ToResult(error)
        };

        return mapped ?? Ok(answer);
    }

    [HttpGet("session/{sessionId:guid}/result")]
    public async Task<IActionResult> GetResult(Guid sessionId, [FromQuery] Guid playerId)
    {
        var (result, error) = await _resultService.GetResultAsync(sessionId, playerId);

        return GameErrorMapper.ToResult(error) ?? Ok(result);
    }
}
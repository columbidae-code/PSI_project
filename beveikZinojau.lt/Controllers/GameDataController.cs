using beveikZinojau.lt.Data;
using beveikZinojau.lt.Services;
using beveikZinojau.lt.Services.ServiceHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace beveikZinojau.lt.Controllers
{
    [ApiController, Route("api")]
    public class GameDataController : ControllerBase
    {
        private GameResultService _gameDataService;
        public GameDataController(GameResultService service)
        {
            _gameDataService = service;
        }

        [HttpGet("leaderboard")]
        public async Task<IActionResult> GetLeaderboard()
        {
            return Ok(await _gameDataService.GetTopNUsersAsync(10));
        }
        [HttpGet("stats/{username}")]
        public async Task<IActionResult> GetUserStats(string username)
        {
              return Ok(await _gameDataService.GetUserStatsAsync(username));
        }
        [HttpGet("stats/{username}/summary")]
        public async Task<IActionResult> GetUserSummary(string username)
        {

            return Ok(await _gameDataService.GetUserSummaryAsync(username));
        }

    }
}

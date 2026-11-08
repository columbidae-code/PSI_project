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
        private GameService _gameDataService;
        public GameDataController(GameService service)
        {
            _gameDataService = service;
        }

        [HttpGet("leaderboard")]
        public async Task<IActionResult> GetLeaderboard()
        {
            return Ok(_gameDataService.GetTopNUsers(10));
        }
        [HttpGet("stats/{username}")]
        public async Task<IActionResult> GetUserStats(string username)
        {
              return Ok(_gameDataService.GetUserStats(username));
        }
        [HttpGet("stats/{username}/summary")]
        public IActionResult GetGameSummarySummary(string username)
        {

            return Ok(_gameDataService.GetUserSummary(username));
        }

    }
}

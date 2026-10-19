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
    [ApiController]
    [Route("api")]


    public class GameController : ControllerBase
    {
        private GameService _service;
        public GameController(GameService service)
        {
            _service = service;
        }

        [HttpGet("leaderboard")]
        public async Task<IActionResult> GetLeaderboard()
        {
            return Ok(_service.GetTopNUsers(10));
        }
        [HttpGet("stats/{username}")]
        public async Task<IActionResult> GetStats(string username)
        {
              return Ok(_service.GetUserStats(username));
        }
        [HttpGet("stats/{username}/summary")]
        public IActionResult GetSummary(string username)
        {

            return Ok(_service.GetUserSummary(username));
        }

    }
}

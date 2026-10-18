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
        public GameService service;
        public GameController(GameService service)
        {
            this.service = service;
        }

        [HttpGet("leaderboard")]
        public async Task<IActionResult> GetLeaderboard()
        {
            return Ok(service.GetTopNUsers(10));
        }
        [HttpGet("stats/{username}")]
        public async Task<IActionResult> GetStats(string username)
        {
              return Ok(service.GetUserStats(username));
        }
        [HttpGet("stats/{username}/summary")]
        public IActionResult GetSummary(string username)
        {

            return Ok(service.GetUserSummary(username));
        }

    }
}

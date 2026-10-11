using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using beveikZinojau.lt.Data;

namespace beveikZinojau.lt.Controllers
{
    [ApiController]
    [Route("api")]


    public class GameController : ControllerBase
    {
        private readonly AppDbContext _db;
        public GameController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("leaderboard")]
        public async Task<IActionResult> GetLeaderboard()
        {
            var result = await _db.GameResults
                  .GroupBy(r => r.User.UserName)
                  .Select(g => new { username = g.Key, score = g.Sum(r => r.Score) })
                  .OrderByDescending(x => x.score)
                  .Take(10)
                  .ToListAsync();

            return Ok(result);
        }
    }
}

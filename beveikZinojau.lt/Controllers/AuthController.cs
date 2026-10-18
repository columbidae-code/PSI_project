using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using beveikZinojau.lt.Data;
using beveikZinojau.lt.Services;

namespace beveikZinojau.lt.Controllers
{


    [ApiController]
    [Route("api")]
    public class AuthController : ControllerBase
    {

        public AuthService service;


        public AuthController(AuthService service)
        {
            this.service = service;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {

            var result = await service.Register(request);
            if (result.Success == false)
            {
                return BadRequest(result);
            } else
            {
                return Ok(result);
            }

        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await service.Login(request);
            if (result.Success == false)
            {
                return Unauthorized(result);
            }
            else
            {
                return Ok(result);
            }
        }
    }
}
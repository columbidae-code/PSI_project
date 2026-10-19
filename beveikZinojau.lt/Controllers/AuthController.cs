using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using beveikZinojau.lt.Data;
using beveikZinojau.lt.Services;
using beveikZinojau.lt.Services.ServiceHelpers;


namespace beveikZinojau.lt.Controllers
{


    [ApiController]
    [Route("api")]
    public class AuthController : ControllerBase
    {

        private AuthService _service;


        public AuthController(AuthService service)
        {
            _service = service;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {

            var result = await _service.Register(request);
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
            var result = await _service.Login(request);
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
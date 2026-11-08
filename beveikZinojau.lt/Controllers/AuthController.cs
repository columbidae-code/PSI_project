using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using beveikZinojau.lt.Data;
using beveikZinojau.lt.Services;
using beveikZinojau.lt.Services.ServiceHelpers;
using Microsoft.AspNetCore.Authorization;


namespace beveikZinojau.lt.Controllers
{


    [ApiController,Route("api")]
    public class AuthController : ControllerBase
    {

        private AuthService _authenticationService;


        public AuthController(AuthService service)
        {
            _authenticationService = service;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {

            var result = await _authenticationService.Register(request);
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
            var result = await _authenticationService.Login(request);
            if (result.Success == false)
            {
                return Unauthorized(result);
            }
            else
            {
                return Ok(result);
            }
        }
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _authenticationService.Logout();
            return Ok();

        }

        [Authorize, HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new { username = User.Identity!.Name });
        }
    }
}
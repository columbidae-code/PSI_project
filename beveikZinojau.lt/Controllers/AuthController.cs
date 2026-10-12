using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using beveikZinojau.lt.Data;

namespace beveikZinojau.lt.Controllers
{
    public class RegisterRequest
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string? Username { get; set; }
    }

    public class StoredUser
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    [ApiController]
    [Route("api")]
    public class AuthController : ControllerBase
    {

        private readonly UserManager<AppUser> _userManager;


        public AuthController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {

            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return BadRequest(new AuthResponse { Success = false, Message = "Username is required." });
            }
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new AuthResponse { Success = false, Message = "Email is required." });
            }
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new AuthResponse { Success = false, Message = "Password is required." });
            }

            if (_userManager.FindByNameAsync(request.Username).Result != null)//Check for lower/upper case not needed
            {

                return BadRequest(new AuthResponse { Success = false, Message = "Username already exists." });
            }

            var newUser = new AppUser
            {
                UserName = request.Username,
                Email = request.Email
            };
            IdentityResult result = await _userManager.CreateAsync(newUser, request.Password);
            if (!result.Succeeded)
            {
                string errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return BadRequest(new AuthResponse { Success = false, Message = errors });
            }
            return Ok(new AuthResponse
            {
                Success = true,
                Message = "User registered successfully.",
                Username = newUser.UserName
            });

        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return Unauthorized(new AuthResponse { Success = false, Message = "Invalid username or password" });

            AppUser user = await _userManager.FindByNameAsync(request.Username);

            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
                return Unauthorized(new AuthResponse { Success = false, Message = "Invalid username or password" });

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "Login successful",
                Username = user.UserName
            });
        }
    }
}
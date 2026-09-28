using Microsoft.AspNetCore.Mvc;

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
        // TODO: Replace the temporary user list with a database.
        private static readonly List<StoredUser> users = new()
        {
            new StoredUser
            {
                Username = "test",
                Email = "test@example.com",
                Password = "1234"
            }
        };

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            bool usernameAlreadyExists = false;

            foreach (StoredUser existingUser in users)
            {
                if (existingUser.Username.ToLower() == request.Username.ToLower())
                {
                    usernameAlreadyExists = true;
                }
            }

            if (usernameAlreadyExists)
            {
                return BadRequest(CreateResponse(false, "Username already exists"));
            }

            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return BadRequest(CreateResponse(false, "Username is required"));
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(CreateResponse(false, "Email is required"));
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(CreateResponse(false, "Password is required"));
            }

            StoredUser newUser = new()
            {
                Username = request.Username,
                Email = request.Email,
                Password = request.Password
            };

            users.Add(newUser);

            return Ok(CreateResponse(
                true,
                "Registration successful",
                newUser.Username
            ));
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            StoredUser? foundUser = null;

            foreach (StoredUser existingUser in users)
            {
                bool usernameMatches = existingUser.Username.ToLower() == request.Username.ToLower();
                bool passwordMatches = existingUser.Password == request.Password;

                if (usernameMatches && passwordMatches)
                {
                    foundUser = existingUser;
                }
            }

            if (foundUser == null)
            {
                return Unauthorized(CreateResponse(false, "Invalid username or password"));
            }

            return Ok(CreateResponse(
                true,
                "Login successful",
                foundUser.Username
            ));
        }

        private static AuthResponse CreateResponse(
            bool success,
            string message,
            string? username = null)
        {
            return new AuthResponse
            {
                Success = success,
                Message = message,
                Username = username
            };
        }
    }
}
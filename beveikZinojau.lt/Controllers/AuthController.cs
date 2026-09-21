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
        public string Username { get; set; }
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
        // Mock database of users. 
        //TODO: create a database and use it instead of this list.
        private static List<StoredUser> users = new List<StoredUser>();

        
        static AuthController()
        {
            StoredUser testUser = new StoredUser();
            testUser.Username = "test";
            testUser.Email = "test@example.com";
            testUser.Password = "1234";
            users.Add(testUser);
        }

        // This method runs when the frontend sends a POST request to /api/register
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
                AuthResponse failResponse = new AuthResponse();
                failResponse.Success = false;
                failResponse.Message = "Username already exists";
                return BadRequest(failResponse);
            }
            if (request.Username == null || request.Username.Trim() == "")
            {
                AuthResponse failResponse = new AuthResponse();
                failResponse.Success = false;
                failResponse.Message = "Username is required";
                return BadRequest(failResponse);
            }
            if (request.Email == null || request.Email.Trim() == "")
            {
                AuthResponse failResponse = new AuthResponse();
                failResponse.Success = false;
                failResponse.Message = "Email is required";
                return BadRequest(failResponse);
            }
            if (request.Password == null || request.Password.Trim() == "")
            {
                AuthResponse failResponse = new AuthResponse();
                failResponse.Success = false;
                failResponse.Message = "Password is required";
                return BadRequest(failResponse);
            }

            // Only happens if username is free and all required fields are provided.
            StoredUser newUser = new StoredUser();
            newUser.Username = request.Username;
            newUser.Email = request.Email;
            newUser.Password = request.Password;
            users.Add(newUser);

            AuthResponse successResponse = new AuthResponse();
            successResponse.Success = true;
            successResponse.Message = "Registration successful";
            successResponse.Username = newUser.Username;
            return Ok(successResponse);
        }

        // This method runs when the frontend sends a POST request to /api/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {

            StoredUser foundUser = null;

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
                AuthResponse failResponse = new AuthResponse();
                failResponse.Success = false;
                failResponse.Message = "Invalid username or password";
                return Unauthorized(failResponse);
            }

            // Only happens if username and password are correct.
            AuthResponse successResponse = new AuthResponse();
            successResponse.Success = true;
            successResponse.Message = "Login successful";
            successResponse.Username = foundUser.Username;
            return Ok(successResponse);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text;

namespace beveikZinojau.lt.Services.ServiceHelpers
{
    public record AuthResponse(bool Success, string Message, string? Username = null)
    {
        public static AuthResponse Ok(string message, string? username = null) => new(true, message, username);

        public static AuthResponse Fail(string message) => new(false, message);
    }
}

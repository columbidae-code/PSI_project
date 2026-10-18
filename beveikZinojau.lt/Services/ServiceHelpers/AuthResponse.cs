using System;
using System.Collections.Generic;
using System.Text;

namespace beveikZinojau.lt.Services.ServiceHelpers
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string? Username { get; set; }
    }
}

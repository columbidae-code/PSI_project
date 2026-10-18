using System;
using System.Collections.Generic;
using System.Text;

namespace beveikZinojau.lt.Services.ServiceHelpers
{
    public record RegisterRequest(string Username, string Email, string Password);
}

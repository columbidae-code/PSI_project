using beveikZinojau.lt.Controllers;
using beveikZinojau.lt.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using beveikZinojau.lt.Services.ServiceHelpers;

namespace beveikZinojau.lt.Services
{

    public class AuthService
    {
        private readonly UserManager<AppUser> _userManager;


        public AuthService(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }
        public async Task<AuthResponse> Register(RegisterRequest request)
        {
            AuthResponse requestResponse = new AuthResponse();

            if (string.IsNullOrWhiteSpace(request.Username))
            {
                requestResponse.Success = false;
                requestResponse.Message = "Username is required.";
                return requestResponse;
            }
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                requestResponse.Success = false;
                requestResponse.Message = "Email is required.";
                return requestResponse;
            }
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                requestResponse.Success = false;
                requestResponse.Message = "Password is required.";
                return requestResponse;
                
            }

            if (_userManager.FindByNameAsync(request.Username) != null)//Check for lower/upper case not needed
            {
                requestResponse.Success = false;
                requestResponse.Message = "Username already exists.";
                return requestResponse;
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
                requestResponse.Success = false;
                requestResponse.Message = errors;
                return requestResponse;
            }
            requestResponse.Success = true;
            requestResponse.Message = "User registered successfully.";
            requestResponse.Username = newUser.UserName;
            return requestResponse;

        }

        public async Task<AuthResponse> Login(LoginRequest request)
        {
            AuthResponse requestResponse = new AuthResponse();

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                requestResponse.Success = false;
                requestResponse.Message = "Invalid username or password";
                return requestResponse;
            }

            AppUser user = await _userManager.FindByNameAsync(request.Username);

            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                requestResponse.Success = false;
                requestResponse.Message = "Invalid username or password";
                return requestResponse;
            }
            requestResponse.Success = true;
            requestResponse.Message = "Login successful";
            requestResponse.Username = user.UserName;

            return requestResponse;
        }

    }
}

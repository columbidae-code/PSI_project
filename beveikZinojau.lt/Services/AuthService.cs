using beveikZinojau.lt.Controllers;
using beveikZinojau.lt.Data;
using beveikZinojau.lt.Services.ServiceHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Supabase.Gotrue;
using System;
using System.Collections.Generic;
using System.Text;

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

            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return AuthResponse.Fail("Username is required.");
            }
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return AuthResponse.Fail("Email is required.");
            }
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return AuthResponse.Fail("Password is required.");
                
            }

            if (_userManager.FindByNameAsync(request.Username) != null)//Check for lower/upper case not needed
            {
                return AuthResponse.Fail("Username already exists.");
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
                return AuthResponse.Fail(errors);

            }
            return AuthResponse.Ok("User registered successfully.", newUser.UserName);

        }

        public async Task<AuthResponse> Login(LoginRequest request)
        {


            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return AuthResponse.Fail("Invalid username or password");
            }

            AppUser user = await _userManager.FindByNameAsync(request.Username);

            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                return AuthResponse.Fail("Invalid username or password");

            }
            return AuthResponse.Ok("Login successful", user.UserName);

        }


    }
}

using beveikZinojau.lt.Controllers;
using beveikZinojau.lt.Data;
using beveikZinojau.lt.Extensions;
using beveikZinojau.lt.Services.ServiceHelpers;
using Microsoft.AspNetCore.Http.HttpResults;
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

        SignInManager<AppUser>  _signInManager;


        public AuthService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }
        public async Task<AuthResponse> Register(RegisterRequest request)
        {

            if (request.Username.IsMissing())
            {
                return AuthResponse.Fail("Username is required.");
            }
            if (request.Email.IsMissing())
            {
                return AuthResponse.Fail("Email is required.");
            }
            if (request.Password.IsMissing())
            {
                return AuthResponse.Fail("Password is required.");
                
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

            if (request.Username.IsMissing() || request.Password.IsMissing())
            {
                return AuthResponse.Fail("Invalid username or password");
            }

            AppUser? user = await _userManager.FindByNameAsync(request.Username);

            if (user == null)
            {
                return AuthResponse.Fail("Invalid username or password");

            }
            // Checks the password and sets the login cookie on the response
            var result = await _signInManager.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: true,
            lockoutOnFailure: true);
            
                        if (result.IsLockedOut)
                        {
                            return AuthResponse.Fail("Too many failed attempts. Try again later.");
                        }

                        if (!result.Succeeded)
                        {
                            return AuthResponse.Fail("Invalid username or password");
            }
            return AuthResponse.Ok("Login successful", user.UserName);

        }

        public Task Logout() { return _signInManager.SignOutAsync(); }

    }
}

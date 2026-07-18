using System.Security.Claims;
using Asp.Versioning;
using HirePathAI.Application.DTOs.Auth;
using HirePathAI.Application.Interfaces;
using HirePathAI.Domain.Constants;
using HirePathAI.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HirePathAI.Web.Controllers.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
    }

    // POST: /api/v1/auth/register
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(
        typeof(AuthResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var normalizedEmail =
            request.Email.Trim().ToLowerInvariant();

        var existingUser =
            await _userManager.FindByEmailAsync(
                normalizedEmail);

        if (existingUser is not null)
        {
            return Conflict(new
            {
                message =
                    "An account already exists with this email address."
            });
        }

        var user = new ApplicationUser
        {
            FullName = request.FullName.Trim(),
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult =
            await _userManager.CreateAsync(
                user,
                request.Password);

        if (!createResult.Succeeded)
        {
            return BadRequest(new
            {
                message = "Registration failed.",

                errors = createResult.Errors
                    .Select(error =>
                        error.Description)
                    .ToArray()
            });
        }

        // Public registration always receives Candidate role.
        var roleResult =
            await _userManager.AddToRoleAsync(
                user,
                UserRoles.Candidate);

        if (!roleResult.Succeeded)
        {
            // Remove the user when role assignment fails.
            await _userManager.DeleteAsync(user);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "The Candidate role could not be assigned.",

                    errors = roleResult.Errors
                        .Select(error =>
                            error.Description)
                        .ToArray()
                });
        }

        var roles =
            await _userManager.GetRolesAsync(user);

        var token =
            _jwtTokenService.GenerateToken(
                user.Id,
                user.FullName,
                user.Email!,
                roles,
                out var expiresAt);

        var response = new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            Token = token,
            ExpiresAt = expiresAt,
            Roles = roles.ToArray()
        };

        return Ok(response);
    }

    // POST: /api/v1/auth/login
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(
        typeof(AuthResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var normalizedEmail =
            request.Email.Trim().ToLowerInvariant();

        var user =
            await _userManager.FindByEmailAsync(
                normalizedEmail);

        if (user is null || !user.IsActive)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        var passwordResult =
            await _signInManager.CheckPasswordSignInAsync(
                user,
                request.Password,
                lockoutOnFailure: true);

        if (passwordResult.IsLockedOut)
        {
            return Unauthorized(new
            {
                message =
                    "Your account is temporarily locked. Please try again later."
            });
        }

        if (passwordResult.IsNotAllowed)
        {
            return Unauthorized(new
            {
                message =
                    "This account is not allowed to sign in."
            });
        }

        if (!passwordResult.Succeeded)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        var roles =
            await _userManager.GetRolesAsync(user);

        var token =
            _jwtTokenService.GenerateToken(
                user.Id,
                user.FullName,
                user.Email!,
                roles,
                out var expiresAt);

        var response = new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            Token = token,
            ExpiresAt = expiresAt,
            Roles = roles.ToArray()
        };

        return Ok(response);
    }

    // GET: /api/v1/auth/me
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        var fullName =
            User.FindFirstValue(
                ClaimTypes.Name);

        var email =
            User.FindFirstValue(
                ClaimTypes.Email);

        var roles =
            User.FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
                .ToArray();

        return Ok(new
        {
            userId,
            fullName,
            email,
            roles
        });
    }
}
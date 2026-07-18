using HirePathAI.Application.DTOs.Auth;
using HirePathAI.Application.Interfaces;
using HirePathAI.Domain.Constants;
using HirePathAI.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HirePathAI.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser>
        _userManager;

    private readonly SignInManager<ApplicationUser>
        _signInManager;

    private readonly IJwtTokenService
        _jwtTokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>>
        Register(RegisterRequest request)
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
                    "An account already exists with this email."
            });
        }

        var user = new ApplicationUser
        {
            FullName = request.FullName.Trim(),
            UserName = normalizedEmail,
            Email = normalizedEmail,
            IsActive = true
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
                errors = createResult.Errors.Select(
                    error => error.Description)
            });
        }

        var roleResult =
            await _userManager.AddToRoleAsync(
                user,
                UserRoles.Candidate);

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "The candidate role could not be assigned.",
                    errors = roleResult.Errors.Select(
                        error => error.Description)
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

        return Ok(new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            Token = token,
            ExpiresAt = expiresAt,
            Roles = roles.ToArray()
        });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>>
        Login(LoginRequest request)
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
                message =
                    "Invalid email or password."
            });
        }

        var passwordResult =
            await _signInManager
                .CheckPasswordSignInAsync(
                    user,
                    request.Password,
                    lockoutOnFailure: true);

        if (passwordResult.IsLockedOut)
        {
            return Unauthorized(new
            {
                message =
                    "The account is temporarily locked."
            });
        }

        if (!passwordResult.Succeeded)
        {
            return Unauthorized(new
            {
                message =
                    "Invalid email or password."
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

        return Ok(new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            Token = token,
            ExpiresAt = expiresAt,
            Roles = roles.ToArray()
        });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        return Ok(new
        {
            userId = User.FindFirst(
                System.Security.Claims
                    .ClaimTypes.NameIdentifier)?.Value,

            fullName = User.Identity?.Name,

            email = User.FindFirst(
                System.Security.Claims
                    .ClaimTypes.Email)?.Value,

            roles = User.FindAll(
                    System.Security.Claims
                        .ClaimTypes.Role)
                .Select(claim => claim.Value)
                .ToArray()
        });
    }
}
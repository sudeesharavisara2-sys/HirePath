using Asp.Versioning;
using HirePathAI.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HirePathAI.Web.Controllers.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/authorization-test")]
[Produces("application/json")]
public class AuthorizationTestController : ControllerBase
{
    // GET: /api/v1/authorization-test/public
    [AllowAnonymous]
    [HttpGet("public")]
    [ProducesResponseType(
        StatusCodes.Status200OK)]
    public IActionResult PublicEndpoint()
    {
        return Ok(new
        {
            message =
                "This is a public endpoint. Authentication is not required.",

            authenticated =
                User.Identity?.IsAuthenticated ?? false,

            serverTimeUtc = DateTime.UtcNow
        });
    }

    // GET: /api/v1/authorization-test/authenticated
    [Authorize]
    [HttpGet("authenticated")]
    [ProducesResponseType(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public IActionResult AuthenticatedEndpoint()
    {
        return Ok(new
        {
            message =
                "JWT authentication is working correctly.",

            userName =
                User.Identity?.Name,

            authenticated =
                User.Identity?.IsAuthenticated ?? false
        });
    }

    // GET: /api/v1/authorization-test/admin
    [Authorize(Roles = UserRoles.Admin)]
    [HttpGet("admin")]
    [ProducesResponseType(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public IActionResult AdminOnly()
    {
        return Ok(new
        {
            message =
                "You have successfully accessed the Admin endpoint.",

            requiredRole = UserRoles.Admin
        });
    }

    // GET: /api/v1/authorization-test/recruiter
    [Authorize(
        Roles =
            UserRoles.Recruiter +
            "," +
            UserRoles.Admin)]
    [HttpGet("recruiter")]
    [ProducesResponseType(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public IActionResult RecruiterOrAdmin()
    {
        return Ok(new
        {
            message =
                "Recruiter or Admin authorization is working.",

            allowedRoles = new[]
            {
                UserRoles.Recruiter,
                UserRoles.Admin
            }
        });
    }

    // GET: /api/v1/authorization-test/candidate
    [Authorize(Roles = UserRoles.Candidate)]
    [HttpGet("candidate")]
    [ProducesResponseType(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public IActionResult CandidateOnly()
    {
        return Ok(new
        {
            message =
                "You have successfully accessed the Candidate endpoint.",

            requiredRole = UserRoles.Candidate
        });
    }

    // GET: /api/v1/authorization-test/hiring-manager
    [Authorize(Roles = UserRoles.HiringManager)]
    [HttpGet("hiring-manager")]
    [ProducesResponseType(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public IActionResult HiringManagerOnly()
    {
        return Ok(new
        {
            message =
                "You have successfully accessed the Hiring Manager endpoint.",

            requiredRole =
                UserRoles.HiringManager
        });
    }

    // GET: /api/v1/authorization-test/management
    [Authorize(
        Roles =
            UserRoles.Admin +
            "," +
            UserRoles.Recruiter +
            "," +
            UserRoles.HiringManager)]
    [HttpGet("management")]
    [ProducesResponseType(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public IActionResult ManagementUsers()
    {
        return Ok(new
        {
            message =
                "Admin, Recruiter and Hiring Manager users can access this endpoint.",

            allowedRoles = new[]
            {
                UserRoles.Admin,
                UserRoles.Recruiter,
                UserRoles.HiringManager
            }
        });
    }
}
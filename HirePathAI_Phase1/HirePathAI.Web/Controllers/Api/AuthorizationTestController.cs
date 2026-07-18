using HirePathAI.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HirePathAI.Web.Controllers.Api;

[ApiController]
[Route("api/authorization-test")]
public class AuthorizationTestController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("public")]
    public IActionResult Public()
    {
        return Ok(new
        {
            message = "Anyone can access this endpoint."
        });
    }

    [Authorize]
    [HttpGet("authenticated")]
    public IActionResult Authenticated()
    {
        return Ok(new
        {
            message =
                "You are authenticated with JWT."
        });
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpGet("admin")]
    public IActionResult AdminOnly()
    {
        return Ok(new
        {
            message =
                "Only administrators can access this endpoint."
        });
    }

    [Authorize(
        Roles = UserRoles.Recruiter +
                "," +
                UserRoles.Admin)]
    [HttpGet("recruiter")]
    public IActionResult RecruiterOrAdmin()
    {
        return Ok(new
        {
            message =
                "Recruiters and administrators can access this endpoint."
        });
    }

    [Authorize(Roles = UserRoles.Candidate)]
    [HttpGet("candidate")]
    public IActionResult CandidateOnly()
    {
        return Ok(new
        {
            message =
                "Only candidates can access this endpoint."
        });
    }

    [Authorize(Roles = UserRoles.HiringManager)]
    [HttpGet("hiring-manager")]
    public IActionResult HiringManagerOnly()
    {
        return Ok(new
        {
            message =
                "Only hiring managers can access this endpoint."
        });
    }
}
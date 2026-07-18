using Asp.Versioning;
using HirePathAI.Application.DTOs.Ats;
using HirePathAI.Application.Interfaces;
using HirePathAI.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HirePathAI.Web.Controllers.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ats")]
[Produces("application/json")]
[Authorize]
public class AtsController : ControllerBase
{
    private readonly IAtsService _atsService;
    private readonly ILogger<AtsController> _logger;

    public AtsController(
        IAtsService atsService,
        ILogger<AtsController> logger)
    {
        _atsService = atsService;
        _logger = logger;
    }

    // POST: /api/v1/ats/analyze
    [HttpPost("analyze")]
    [Authorize(
        Roles =
            UserRoles.Admin + "," +
            UserRoles.Recruiter + "," +
            UserRoles.HiringManager)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(
        typeof(AtsAnalysisResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AtsAnalysisResponse>>
        AnalyzeResume(
            [FromForm] int jobId,
            [FromForm] IFormFile resumeFile,
            CancellationToken cancellationToken)
    {
        if (jobId <= 0)
        {
            return BadRequest(new
            {
                message = "A valid job ID is required."
            });
        }

        if (resumeFile is null ||
            resumeFile.Length == 0)
        {
            return BadRequest(new
            {
                message = "Please upload a resume PDF."
            });
        }

        try
        {
            var response =
                await _atsService.AnalyzeResumeAsync(
                    jobId,
                    resumeFile,
                    cancellationToken);

            return Ok(response);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new
            {
                message = exception.Message
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(
                exception,
                "ATS analysis could not be completed.");

            return StatusCode(
                StatusCodes.Status422UnprocessableEntity,
                new
                {
                    message = exception.Message
                });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unexpected ATS analysis error.");

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "An unexpected error occurred while analysing the resume."
                });
        }
    }

    // GET: /api/v1/ats/analyses
    [HttpGet("analyses")]
    [Authorize(
        Roles =
            UserRoles.Admin + "," +
            UserRoles.Recruiter + "," +
            UserRoles.HiringManager)]
    [ProducesResponseType(
        typeof(
            IReadOnlyCollection<
                AtsAnalysisSummaryResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<
        IReadOnlyCollection<
            AtsAnalysisSummaryResponse>>>
        GetAllAnalyses(
            CancellationToken cancellationToken)
    {
        var results =
            await _atsService.GetAllAnalysesAsync(
                cancellationToken);

        return Ok(results);
    }

    // GET: /api/v1/ats/analyses/5
    [HttpGet("analyses/{id:int}")]
    [Authorize(
        Roles =
            UserRoles.Admin + "," +
            UserRoles.Recruiter + "," +
            UserRoles.HiringManager)]
    [ProducesResponseType(
        typeof(AtsAnalysisResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AtsAnalysisResponse>>
        GetAnalysisById(
            int id,
            CancellationToken cancellationToken)
    {
        var result =
            await _atsService.GetAnalysisByIdAsync(
                id,
                cancellationToken);

        if (result is null)
        {
            return NotFound(new
            {
                message =
                    $"Analysis with ID {id} was not found."
            });
        }

        return Ok(result);
    }

    // GET: /api/v1/ats/jobs/3/ranking
    [HttpGet("jobs/{jobId:int}/ranking")]
    [Authorize(
        Roles =
            UserRoles.Admin + "," +
            UserRoles.Recruiter + "," +
            UserRoles.HiringManager)]
    [ProducesResponseType(
        typeof(
            IReadOnlyCollection<
                CandidateRankingResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<
        IReadOnlyCollection<
            CandidateRankingResponse>>>
        GetCandidateRanking(
            int jobId,
            CancellationToken cancellationToken)
    {
        try
        {
            var results =
                await _atsService
                    .GetCandidateRankingAsync(
                        jobId,
                        cancellationToken);

            return Ok(results);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new
            {
                message = exception.Message
            });
        }
    }

    // DELETE: /api/v1/ats/analyses/5
    [HttpDelete("analyses/{id:int}")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAnalysis(
        int id,
        CancellationToken cancellationToken)
    {
        var deleted =
            await _atsService.DeleteAnalysisAsync(
                id,
                cancellationToken);

        if (!deleted)
        {
            return NotFound(new
            {
                message =
                    $"Analysis with ID {id} was not found."
            });
        }

        return NoContent();
    }
}
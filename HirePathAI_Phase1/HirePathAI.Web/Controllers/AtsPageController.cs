using HirePathAI.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HirePathAI.Web.Controllers;

[Authorize]
public class AtsPageController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public AtsPageController(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        ViewBag.Jobs =
            await _dbContext.Jobs
                .AsNoTracking()
                .Where(job => job.IsActive)
                .OrderByDescending(job =>
                    job.CreatedAt)
                .Select(job => new
                {
                    job.Id,
                    job.Title,
                    job.CompanyName
                })
                .ToListAsync(cancellationToken);

        return View();
    }
}
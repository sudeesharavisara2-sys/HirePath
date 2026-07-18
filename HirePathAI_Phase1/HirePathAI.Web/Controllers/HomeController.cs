using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HirePathAI.Application.Interfaces;
using HirePathAI.Presentation.Models;

namespace HirePathAI.Presentation.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IAtsDashboardStore _dashboardStore;

    public HomeController(ILogger<HomeController> logger, IAtsDashboardStore dashboardStore)
    {
        _logger = logger;
        _dashboardStore = dashboardStore;
    }

    public IActionResult Index()
    {
        var summary = _dashboardStore.GetSummary();
        ViewBag.TotalResumes = summary.TotalProcessed;
        ViewBag.SelectedCount = summary.SelectedCount;
        ViewBag.ConsiderCount = summary.ConsiderCount;
        ViewBag.RejectedCount = summary.RejectedCount;
        ViewBag.AvgScore = Math.Round(summary.AverageOverallScore);
        ViewBag.SelectedPercent = Math.Round(summary.SelectedPercent);
        ViewBag.AiAccuracy = Math.Round(summary.AverageMlConfidencePercent);
        ViewBag.LatestCandidates = summary.LatestCandidates;
        ViewBag.TopCandidates = summary.TopCandidates;
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

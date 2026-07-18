using Microsoft.AspNetCore.Mvc;
using HirePathAI.Application.Interfaces;
using HirePathAI.Application.DTOs;
using HirePathAI.Presentation.Models;

namespace HirePathAI.Presentation.Controllers
{
    public class JobController : Controller
    {
        private readonly IJobStore _jobStore;

        public JobController(IJobStore jobStore)
        {
            _jobStore = jobStore;
        }

        public async Task<IActionResult> Index()
        {
            var jobs = await _jobStore.GetAllJobsAsync();
            return View(jobs);
        }

        public IActionResult Create()
        {
            return View(new JobCreationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobCreationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var job = new JobRequirementDto
            {
                Title = model.Title,
                CompanyName = model.CompanyName,
                Department = model.Department,
                RequiredSkills = ParseCsv(model.RequiredSkillsCsv),
                PreferredSkills = ParseCsv(model.OptionalSkillsCsv),
                MinimumYearsExperience = model.MinimumYearsExperience,
                MinimumEducationLevel = model.MinimumEducationLevel
            };

            await _jobStore.CreateJobAsync(job);

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Details(string id)
        {
            var job = await _jobStore.GetJobAsync(id);
            if (job == null) return NotFound();
            return View(job);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var job = await _jobStore.GetJobAsync(id);
            if (job == null) return NotFound();

            var model = new JobCreationViewModel
            {
                Title = job.Title,
                CompanyName = job.CompanyName,
                Department = job.Department,
                RequiredSkillsCsv = string.Join(", ", job.RequiredSkills),
                OptionalSkillsCsv = string.Join(", ", job.PreferredSkills),
                MinimumYearsExperience = job.MinimumYearsExperience,
                MinimumEducationLevel = job.MinimumEducationLevel
            };
            ViewBag.JobId = job.Id;
            return View("Create", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, JobCreationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.JobId = id;
                return View("Create", model);
            }

            var job = await _jobStore.GetJobAsync(id);
            if (job == null) return NotFound();

            job.Title = model.Title;
            job.CompanyName = model.CompanyName;
            job.Department = model.Department;
            job.RequiredSkills = ParseCsv(model.RequiredSkillsCsv);
            job.PreferredSkills = ParseCsv(model.OptionalSkillsCsv);
            job.MinimumYearsExperience = model.MinimumYearsExperience;
            job.MinimumEducationLevel = model.MinimumEducationLevel;

            await _jobStore.UpdateJobAsync(job);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            await _jobStore.DeleteJobAsync(id);
            return RedirectToAction(nameof(Index));
        }
        private List<string> ParseCsv(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
                return new List<string>();

            return csv.Split(',')
                      .Select(s => s.Trim())
                      .Where(s => !string.IsNullOrEmpty(s))
                      .ToList();
        }
    }
}

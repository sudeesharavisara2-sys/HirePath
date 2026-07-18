using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using HirePathAI.Infrastructure.AI.MLModels;
using HirePathAI.Infrastructure.AI.Services;
using HirePathAI.Application.Interfaces;
using HirePathAI.Domain.Entities;

namespace HirePathAI.Presentation.Controllers
{
    public class ResumeController : Controller
    {
        private readonly ResumeService _resumeService;
        private readonly PdfService _pdfService;
        private readonly IPdfExtractor _enhancedPdf;
        private readonly IResumeParser _resumeParser;
        private readonly IResumeScorer _resumeScorer;
        private readonly IAtsDashboardStore _dashboardStore;
        private readonly IAtsAnalysisResultStore _resultStore;
        private readonly IJobStore _jobStore;
        private readonly ILogger<ResumeController> _logger;

        public ResumeController(
            ResumeService resumeService,
            PdfService pdfService,
            IPdfExtractor enhancedPdf,
            IResumeParser resumeParser,
            IResumeScorer resumeScorer,
            IAtsDashboardStore dashboardStore,
            IAtsAnalysisResultStore resultStore,
            IJobStore jobStore,
            ILogger<ResumeController> logger)
        {
            _resumeService = resumeService;
            _pdfService = pdfService;
            _enhancedPdf = enhancedPdf;
            _resumeParser = resumeParser;
            _resumeScorer = resumeScorer;
            _dashboardStore = dashboardStore;
            _resultStore = resultStore;
            _jobStore = jobStore;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.Jobs = await _jobStore.GetAllJobsAsync();
            return View("~/Views/Resume/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> Result(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !_resultStore.TryTake(id, out var result))
            {
                ViewBag.Error = "Result expired or not found. Please run the analysis again.";
                ViewBag.Jobs = await _jobStore.GetAllJobsAsync();
                return View("~/Views/Resume/Index.cshtml");
            }

            ApplyResultToViewBag(result);
            ViewBag.Jobs = await _jobStore.GetAllJobsAsync();
            return View("~/Views/Resume/Index.cshtml");
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AnalyzeStream(string resumeText, IFormFile resumeFile, string jobId)
        {
            Response.Headers["Content-Type"] = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";

            async Task EmitAsync(string evt, object payload)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                await Response.WriteAsync($"event: {evt}\n");
                await Response.WriteAsync($"data: {json}\n\n");
                await Response.Body.FlushAsync();
            }

            try
            {
                var analysis = await AnalyzeInternalAsync(
                    resumeText,
                    resumeFile,
                    jobId,
                    async (key, state) => await EmitAsync("step", new { key, state, ts = DateTimeOffset.UtcNow }),
                    HttpContext.RequestAborted);

                var resultId = _resultStore.Put(analysis.Result);
                await EmitAsync("done", new { resultId });
                return new EmptyResult();
            }
            catch (OperationCanceledException)
            {
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Streaming analysis failed");
                await EmitAsync("error", new { message = "Analysis failed. Please try again." });
                return new EmptyResult();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Index(string resumeText, IFormFile resumeFile, string jobId)
        {
            try
            {
                var analysis = AnalyzeInternalAsync(resumeText, resumeFile, jobId, (_, _) => Task.CompletedTask, default).GetAwaiter().GetResult();
                ApplyResultToViewBag(analysis.Result);

                _logger.LogInformation("Resume analysis completed: Decision={Decision}, Confidence={Confidence:P2}, OverallScore={Score:F1}",
                    analysis.Result.Decision, analysis.Result.ScoreConfidence, analysis.Result.ScoreOverall);
                
                ViewBag.Jobs = await _jobStore.GetAllJobsAsync();
                return View("~/Views/Resume/Index.cshtml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during resume analysis");
                ViewBag.Error = "An error occurred while processing your resume. Please try again.";
                ViewBag.Jobs = await _jobStore.GetAllJobsAsync();
                return View("~/Views/Resume/Index.cshtml");
            }
        }

        private sealed record AnalysisBundle(AtsAnalysisResult Result);

        private async Task<AnalysisBundle> AnalyzeInternalAsync(
            string resumeText,
            IFormFile resumeFile,
            string jobId,
            Func<string, string, Task> step,
            CancellationToken ct)
        {
            var job = await _jobStore.GetJobAsync(jobId) ?? (await _jobStore.GetAllJobsAsync()).FirstOrDefault();
            if (job == null) throw new InvalidOperationException("No job requirement found to screen against.");
            string extractedText;

            if (string.IsNullOrWhiteSpace(resumeText) && resumeFile == null)
            {
                throw new InvalidOperationException("Empty input");
            }

            if (resumeFile != null)
            {
                await step("resume_upload", "active");
                var fileValidation = _enhancedPdf.Validate(resumeFile);
                if (!fileValidation.IsValid)
                {
                    throw new InvalidOperationException(fileValidation.ErrorMessage);
                }

                extractedText = _enhancedPdf.ExtractText(resumeFile);
                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    extractedText = _pdfService.ExtractTextFromPdf(resumeFile);
                }
                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    throw new InvalidOperationException("Could not extract text from the PDF file. Please ensure the PDF contains readable text.");
                }
            }
            else
            {
                await step("resume_upload", "active");
                extractedText = resumeText;
            }

            await step("resume_upload", "done");
            ct.ThrowIfCancellationRequested();

            await step("resume_parsing", "active");
            var cleanText = System.Text.RegularExpressions.Regex.Replace(extractedText, @"--- PAGE \d+ ---\s*", "");
            cleanText = System.Text.RegularExpressions.Regex.Replace(cleanText, @"[ ]{2,}", " ").Trim();
            await step("resume_parsing", "done");
            ct.ThrowIfCancellationRequested();

            var resumeValidation = _pdfService.IsProbablyResume(cleanText);
            if (!resumeValidation.IsResume)
            {
                throw new InvalidOperationException("Invalid Document - " + resumeValidation.Reason);
            }

            await step("mlnet_prediction", "active");
            var prediction = _resumeService.Predict(cleanText);
            await step("mlnet_prediction", "done");
            ct.ThrowIfCancellationRequested();

            ParsedResume parsedResume;
            try
            {
                await step("skills_extraction", "active");
                parsedResume = _resumeParser.Parse(cleanText, job);
                await step("skills_extraction", "done");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Resume parsing failed, continuing with ML only");
                parsedResume = new ParsedResume { FullText = extractedText };
                await step("skills_extraction", "done");
            }

            static string FormatEdu(EducationEntry e)
            {
                var degree = string.IsNullOrWhiteSpace(e.Degree) ? null : e.Degree.Trim();
                var field = string.IsNullOrWhiteSpace(e.Field) ? null : e.Field.Trim();
                var inst = string.IsNullOrWhiteSpace(e.Institution) ? null : e.Institution.Trim();
                var year = string.IsNullOrWhiteSpace(e.Year) ? null : e.Year.Trim();

                var left = string.Join(" ", new[] { degree, field }.Where(s => !string.IsNullOrWhiteSpace(s)));
                var right = string.Join(" ", new[] { inst, year is null ? null : $"({year})" }.Where(s => !string.IsNullOrWhiteSpace(s)));

                var joined = string.Join(" - ", new[] { left, right }.Where(s => !string.IsNullOrWhiteSpace(s)));
                return string.IsNullOrWhiteSpace(joined) ? "Education entry" : joined;
            }

            static string FormatExp(ExperienceEntry e)
            {
                var title = string.IsNullOrWhiteSpace(e.JobTitle) ? "Professional Role" : e.JobTitle.Trim();
                var company = string.IsNullOrWhiteSpace(e.Company) ? "Unknown Company" : e.Company.Trim();
                var duration = string.IsNullOrWhiteSpace(e.Duration) ? "Unknown Duration" : e.Duration.Trim();

                return $"<strong style='color:#E5E7EB;font-size:0.9rem;'>{title}</strong><br/><span style='color:#94A3B8;font-size:0.85rem;'><i class='fas fa-building me-1'></i> {company}</span><br/><span style='color:#6B7280;font-size:0.8rem;'><i class='fas fa-calendar-alt me-1'></i> {duration}</span>";
            }

            static string FormatProject(ProjectEntry p)
            {
                var name = string.IsNullOrWhiteSpace(p.Name) ? "Project" : p.Name.Trim();
                var tech = p.Technologies?.Count > 0 ? string.Join(", ", p.Technologies.Take(5)) : null;
                return string.IsNullOrWhiteSpace(tech) ? name : $"{name} [{tech}]";
            }

            await step("experience_analysis", "active");
            var experienceFormatted = parsedResume.Experience.Select(FormatExp).ToList();
            await step("experience_analysis", "done");

            await step("education_analysis", "active");
            var educationFormatted = parsedResume.Education.Select(FormatEdu).ToList();
            await step("education_analysis", "done");

            await step("certification_analysis", "active");
            var certifications = parsedResume.Certifications.ToList();
            await step("certification_analysis", "done");

            ct.ThrowIfCancellationRequested();

            await step("ats_scoring", "active");
            var resumeScore = _resumeScorer.Score(parsedResume, job, prediction.Probability);
            await step("ats_scoring", "done");

            var decision = resumeScore.Decision ?? prediction.Decision ?? "Unknown";
            await step("decision_engine", "active");
            await step("decision_engine", "done");
            await step("final_result", "active");
            await step("final_result", "done");

            _dashboardStore.Add(new AtsCandidateSnapshot(
                CandidateName: string.IsNullOrWhiteSpace(parsedResume.Name) ? "Candidate" : parsedResume.Name,
                Decision: decision,
                OverallScore: resumeScore.OverallScore,
                MlConfidence: prediction.Probability,
                Timestamp: DateTimeOffset.UtcNow,
                ExperienceCount: parsedResume.Experience?.Count ?? 0,
                SkillsCount: parsedResume.Skills?.Count ?? 0));

            var result = new AtsAnalysisResult(
                ResumeText: cleanText,
                Selected: prediction.Selected,
                Decision: decision,
                ScoreConfidence: prediction.Probability,
                IsPdfUpload: resumeFile != null,
                ParsedName: parsedResume.Name,
                ParsedEmail: parsedResume.Email,
                ParsedPhone: parsedResume.Phone,
                ParsedSkills: parsedResume.Skills,
                ParsedEducation: educationFormatted,
                ParsedExperience: experienceFormatted,
                ParsedCertifications: certifications,
                ParsedProjects: parsedResume.Projects.Select(FormatProject).ToList(),
                ScoreOverall: resumeScore.OverallScore,
                ScoreSkills: resumeScore.SkillsScore,
                ScoreExperience: resumeScore.ExperienceScore,
                ScoreEducation: resumeScore.EducationScore,
                ScoreProject: resumeScore.ProjectScore,
                ScoreCertification: resumeScore.CertificationScore,
                Strengths: resumeScore.Strengths,
                Weaknesses: resumeScore.Weaknesses,
                MissingSkills: resumeScore.MissingSkills,
                Recommendation: resumeScore.Recommendation);

            return new AnalysisBundle(result);
        }

        private void ApplyResultToViewBag(AtsAnalysisResult result)
        {
            ViewBag.ResumeText = result.ResumeText;
            ViewBag.Selected = result.Selected;
            ViewBag.Decision = result.Decision;
            ViewBag.ScoreConfidence = result.ScoreConfidence * 100;
            ViewBag.IsPdfUpload = result.IsPdfUpload;

            ViewBag.ParsedName = result.ParsedName;
            ViewBag.ParsedEmail = result.ParsedEmail;
            ViewBag.ParsedPhone = result.ParsedPhone;
            ViewBag.ParsedSkills = result.ParsedSkills.ToList();
            ViewBag.ParsedEducation = result.ParsedEducation.ToList();
            ViewBag.ParsedExperience = result.ParsedExperience.ToList();
            ViewBag.ParsedCertifications = result.ParsedCertifications.ToList();
            ViewBag.ParsedProjects = result.ParsedProjects.ToList();

            ViewBag.ScoreOverall = result.ScoreOverall;
            ViewBag.ScoreSkills = result.ScoreSkills;
            ViewBag.ScoreExperience = result.ScoreExperience;
            ViewBag.ScoreEducation = result.ScoreEducation;
            ViewBag.ScoreProject = result.ScoreProject;
            ViewBag.ScoreCertification = result.ScoreCertification;
            ViewBag.Strengths = result.Strengths.ToList();
            ViewBag.Weaknesses = result.Weaknesses.ToList();
            ViewBag.MissingSkills = result.MissingSkills.ToList();
            ViewBag.Recommendation = result.Recommendation;
        }
    }
}

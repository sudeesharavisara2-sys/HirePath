using System.Text.Json;
using HirePathAI.Application.DTOs.Ats;
using HirePathAI.Application.Interfaces;
using HirePathAI.Domain.Entities;
using HirePathAI.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HirePathAI.Infrastructure.Services;

public class AtsService : IAtsService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPdfExtractor _pdfExtractor;
    private readonly IPublicAiResumeService _publicAiService;
    private readonly ILogger<AtsService> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public AtsService(
        ApplicationDbContext dbContext,
        IPdfExtractor pdfExtractor,
        IPublicAiResumeService publicAiService,
        ILogger<AtsService> logger)
    {
        _dbContext = dbContext;
        _pdfExtractor = pdfExtractor;
        _publicAiService = publicAiService;
        _logger = logger;
    }

    public async Task<AtsAnalysisResponse>
        AnalyzeResumeAsync(
            int jobId,
            IFormFile resumeFile,
            CancellationToken cancellationToken = default)
    {
        if (jobId <= 0)
        {
            throw new ArgumentException(
                "A valid job ID is required.",
                nameof(jobId));
        }

        var validation =
            _pdfExtractor.Validate(resumeFile);

        if (!validation.IsValid)
        {
            throw new ArgumentException(
                validation.ErrorMessage,
                nameof(resumeFile));
        }

        var job =
            await _dbContext.Jobs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.Id == jobId,
                    cancellationToken);

        if (job is null)
        {
            throw new KeyNotFoundException(
                $"Job with ID {jobId} was not found.");
        }

        if (!job.IsActive)
        {
            throw new InvalidOperationException(
                "This job is no longer active.");
        }

        var resumeText =
            _pdfExtractor.ExtractText(resumeFile);

        if (string.IsNullOrWhiteSpace(resumeText))
        {
            throw new InvalidOperationException(
                "No readable text could be extracted from the resume.");
        }

        var resumeCheck =
            _pdfExtractor.IsResumeDocument(resumeText);

        if (!resumeCheck.IsResume)
        {
            throw new ArgumentException(
                $"The uploaded file does not appear to be a resume. " +
                $"{resumeCheck.Reason}",
                nameof(resumeFile));
        }

        var jobDescription =
            BuildJobDescription(job);

        AiResumeAnalysisResult aiResult;

        try
        {
            aiResult =
                await _publicAiService.AnalyzeResumeAsync(
                    resumeText,
                    jobDescription,
                    cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "AI analysis failed for job {JobId} and file {FileName}.",
                jobId,
                resumeFile.FileName);

            throw new InvalidOperationException(
                "The AI service could not analyse the resume.",
                exception);
        }

        NormalizeAiResult(aiResult);

        var analysis = new ResumeAnalysis
        {
            JobId = job.Id,

            CandidateName =
                string.IsNullOrWhiteSpace(
                    aiResult.CandidateName)
                    ? "Unknown Candidate"
                    : aiResult.CandidateName.Trim(),

            CandidateEmail =
                NullIfEmpty(aiResult.CandidateEmail),

            CandidatePhone =
                NullIfEmpty(aiResult.CandidatePhone),

            ResumeFileName =
                Path.GetFileName(resumeFile.FileName),

            ExtractedSkills =
                Serialize(aiResult.ExtractedSkills),

            MatchedSkills =
                Serialize(aiResult.MatchedSkills),

            MissingSkills =
                Serialize(aiResult.MissingSkills),

            Education =
                Serialize(aiResult.Education),

            Experience =
                Serialize(aiResult.Experience),

            Certifications =
                Serialize(aiResult.Certifications),

            TotalExperienceYears =
                aiResult.TotalExperienceYears,

            SkillsScore =
                ClampScore(aiResult.SkillsScore),

            ExperienceScore =
                ClampScore(aiResult.ExperienceScore),

            EducationScore =
                ClampScore(aiResult.EducationScore),

            CertificationScore =
                ClampScore(aiResult.CertificationScore),

            AtsScore =
                ClampScore(aiResult.AtsScore),

            MatchPercentage =
                ClampScore(aiResult.MatchPercentage),

            Recommendation =
                aiResult.Recommendation.Trim(),

            Summary =
                aiResult.Summary.Trim(),

            ProcessedAt = DateTime.UtcNow
        };

        _dbContext.ResumeAnalyses.Add(analysis);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        analysis.Job = job;

        return MapToResponse(analysis);
    }

    public async Task<
        IReadOnlyCollection<AtsAnalysisSummaryResponse>>
        GetAllAnalysesAsync(
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.ResumeAnalyses
            .AsNoTracking()
            .OrderByDescending(item => item.ProcessedAt)
            .Select(item =>
                new AtsAnalysisSummaryResponse
                {
                    Id = item.Id,
                    JobId = item.JobId,
                    JobTitle = item.Job.Title,
                    CandidateName = item.CandidateName,
                    CandidateEmail = item.CandidateEmail,
                    ResumeFileName = item.ResumeFileName,
                    AtsScore = item.AtsScore,
                    MatchPercentage =
                        item.MatchPercentage,
                    Recommendation =
                        item.Recommendation,
                    ProcessedAt =
                        item.ProcessedAt
                })
            .ToListAsync(cancellationToken);
    }

    public async Task<AtsAnalysisResponse?>
        GetAnalysisByIdAsync(
            int analysisId,
            CancellationToken cancellationToken = default)
    {
        var analysis =
            await _dbContext.ResumeAnalyses
                .AsNoTracking()
                .Include(item => item.Job)
                .FirstOrDefaultAsync(
                    item => item.Id == analysisId,
                    cancellationToken);

        return analysis is null
            ? null
            : MapToResponse(analysis);
    }

    public async Task<
        IReadOnlyCollection<CandidateRankingResponse>>
        GetCandidateRankingAsync(
            int jobId,
            CancellationToken cancellationToken = default)
    {
        var jobExists =
            await _dbContext.Jobs
                .AnyAsync(
                    item => item.Id == jobId,
                    cancellationToken);

        if (!jobExists)
        {
            throw new KeyNotFoundException(
                $"Job with ID {jobId} was not found.");
        }

        var analyses =
            await _dbContext.ResumeAnalyses
                .AsNoTracking()
                .Where(item => item.JobId == jobId)
                .OrderByDescending(item =>
                    item.AtsScore)
                .ThenByDescending(item =>
                    item.MatchPercentage)
                .ThenByDescending(item =>
                    item.ProcessedAt)
                .ToListAsync(cancellationToken);

        return analyses
            .Select((item, index) =>
                new CandidateRankingResponse
                {
                    Rank = index + 1,
                    AnalysisId = item.Id,
                    JobId = item.JobId,
                    CandidateName =
                        item.CandidateName,
                    CandidateEmail =
                        item.CandidateEmail,
                    AtsScore = item.AtsScore,
                    MatchPercentage =
                        item.MatchPercentage,
                    Recommendation =
                        item.Recommendation,
                    MatchedSkills =
                        Deserialize(item.MatchedSkills),
                    MissingSkills =
                        Deserialize(item.MissingSkills),
                    ProcessedAt =
                        item.ProcessedAt
                })
            .ToList();
    }

    public async Task<bool> DeleteAnalysisAsync(
        int analysisId,
        CancellationToken cancellationToken = default)
    {
        var analysis =
            await _dbContext.ResumeAnalyses
                .FirstOrDefaultAsync(
                    item => item.Id == analysisId,
                    cancellationToken);

        if (analysis is null)
        {
            return false;
        }

        _dbContext.ResumeAnalyses.Remove(analysis);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    private static AtsAnalysisResponse MapToResponse(
        ResumeAnalysis analysis)
    {
        return new AtsAnalysisResponse
        {
            Id = analysis.Id,
            JobId = analysis.JobId,
            JobTitle =
                analysis.Job?.Title ?? string.Empty,
            CompanyName =
                analysis.Job?.CompanyName ?? string.Empty,
            CandidateName =
                analysis.CandidateName,
            CandidateEmail =
                analysis.CandidateEmail,
            CandidatePhone =
                analysis.CandidatePhone,
            ResumeFileName =
                analysis.ResumeFileName,
            ExtractedSkills =
                Deserialize(
                    analysis.ExtractedSkills),
            MatchedSkills =
                Deserialize(
                    analysis.MatchedSkills),
            MissingSkills =
                Deserialize(
                    analysis.MissingSkills),
            Education =
                Deserialize(analysis.Education),
            Experience =
                Deserialize(analysis.Experience),
            Certifications =
                Deserialize(
                    analysis.Certifications),
            TotalExperienceYears =
                analysis.TotalExperienceYears,
            SkillsScore =
                analysis.SkillsScore,
            ExperienceScore =
                analysis.ExperienceScore,
            EducationScore =
                analysis.EducationScore,
            CertificationScore =
                analysis.CertificationScore,
            AtsScore =
                analysis.AtsScore,
            MatchPercentage =
                analysis.MatchPercentage,
            Recommendation =
                analysis.Recommendation,
            Summary =
                analysis.Summary,
            ProcessedAt =
                analysis.ProcessedAt
        };
    }

    private static string BuildJobDescription(
        Job job)
    {
        return $"""
                Job title: {job.Title}
                Company: {job.CompanyName}
                Location: {job.Location}
                Description: {job.Description}
                Required skills: {job.RequiredSkills}
                Preferred skills: {job.PreferredSkills}
                Minimum experience: {job.MinimumExperienceYears} years
                Minimum education: {job.MinimumEducation}
                """;
    }

    private static void NormalizeAiResult(
        AiResumeAnalysisResult result)
    {
        result.ExtractedSkills ??= [];
        result.MatchedSkills ??= [];
        result.MissingSkills ??= [];
        result.Education ??= [];
        result.Experience ??= [];
        result.Certifications ??= [];

        result.CandidateName ??= string.Empty;
        result.CandidateEmail ??= string.Empty;
        result.CandidatePhone ??= string.Empty;
        result.Recommendation ??= string.Empty;
        result.Summary ??= string.Empty;
    }

    private static double ClampScore(
        double value)
    {
        return Math.Round(
            Math.Clamp(value, 0, 100),
            2);
    }

    private static string Serialize(
        IEnumerable<string>? values)
    {
        var cleanValues =
            values?
                .Where(value =>
                    !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList()
            ?? [];

        return JsonSerializer.Serialize(
            cleanValues,
            JsonOptions);
    }

    private static List<string> Deserialize(
        string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(
                       json,
                       JsonOptions)
                   ?? [];
        }
        catch (JsonException)
        {
            return json
                .Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .ToList();
        }
    }

    private static string? NullIfEmpty(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
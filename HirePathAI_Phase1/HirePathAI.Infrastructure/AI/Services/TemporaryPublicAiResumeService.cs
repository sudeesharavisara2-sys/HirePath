using System.Text.RegularExpressions;
using HirePathAI.Application.DTOs.Ats;
using HirePathAI.Application.Interfaces;

namespace HirePathAI.Infrastructure.AI.Services;

public class TemporaryPublicAiResumeService
    : IPublicAiResumeService
{
    private static readonly string[] KnownSkills =
    [
        "C#",
        ".NET",
        "ASP.NET Core",
        "Java",
        "Spring Boot",
        "JavaScript",
        "TypeScript",
        "React",
        "Angular",
        "Vue",
        "SQL",
        "SQL Server",
        "PostgreSQL",
        "MySQL",
        "MongoDB",
        "Docker",
        "Azure",
        "AWS",
        "Git",
        "REST API",
        "Entity Framework",
        "Python",
        "Machine Learning",
        "HTML",
        "CSS"
    ];

    public Task<AiResumeAnalysisResult> AnalyzeResumeAsync(
        string resumeText,
        string jobDescription,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(resumeText))
        {
            throw new ArgumentException(
                "Resume text cannot be empty.",
                nameof(resumeText));
        }

        var extractedSkills = KnownSkills
            .Where(skill =>
                resumeText.Contains(
                    skill,
                    StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var requiredSkills = KnownSkills
            .Where(skill =>
                jobDescription.Contains(
                    skill,
                    StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var matchedSkills = extractedSkills
            .Where(skill =>
                requiredSkills.Contains(
                    skill,
                    StringComparer.OrdinalIgnoreCase))
            .ToList();

        var missingSkills = requiredSkills
            .Where(skill =>
                !extractedSkills.Contains(
                    skill,
                    StringComparer.OrdinalIgnoreCase))
            .ToList();

        var matchPercentage = requiredSkills.Count == 0
            ? 50
            : Math.Round(
                matchedSkills.Count * 100.0 /
                requiredSkills.Count,
                2);

        var candidateName =
            ExtractCandidateName(resumeText);

        var email =
            Regex.Match(
                resumeText,
                @"[\w\.-]+@[\w\.-]+\.\w+")
            .Value;

        var phone =
            Regex.Match(
                resumeText,
                @"(?:\+?\d[\d\s\-]{7,}\d)")
            .Value;

        var result = new AiResumeAnalysisResult
        {
            CandidateName = candidateName,
            CandidateEmail = email,
            CandidatePhone = phone,

            ExtractedSkills = extractedSkills,
            MatchedSkills = matchedSkills,
            MissingSkills = missingSkills,

            Education =
                ExtractLinesContaining(
                    resumeText,
                    [
                        "degree",
                        "bsc",
                        "bachelor",
                        "master",
                        "diploma",
                        "university"
                    ]),

            Experience =
                ExtractLinesContaining(
                    resumeText,
                    [
                        "experience",
                        "developer",
                        "engineer",
                        "intern",
                        "manager"
                    ]),

            Certifications =
                ExtractLinesContaining(
                    resumeText,
                    [
                        "certificate",
                        "certification",
                        "certified"
                    ]),

            TotalExperienceYears = 0,

            SkillsScore = matchPercentage,

            ExperienceScore =
                resumeText.Contains(
                    "experience",
                    StringComparison.OrdinalIgnoreCase)
                    ? 70
                    : 40,

            EducationScore =
                resumeText.Contains(
                    "university",
                    StringComparison.OrdinalIgnoreCase)
                    ? 75
                    : 50,

            CertificationScore =
                resumeText.Contains(
                    "certif",
                    StringComparison.OrdinalIgnoreCase)
                    ? 70
                    : 40,

            AtsScore = Math.Round(
                matchPercentage * 0.60 +
                70 * 0.20 +
                75 * 0.15 +
                50 * 0.05,
                2),

            MatchPercentage = matchPercentage,

            Recommendation =
                matchPercentage >= 75
                    ? "Highly Recommended"
                    : matchPercentage >= 50
                        ? "Consider for Review"
                        : "Not Recommended",

            Summary =
                "This is a temporary ATS response. " +
                "The public AI implementation will replace this service."
        };

        return Task.FromResult(result);
    }

    private static string ExtractCandidateName(
        string resumeText)
    {
        var firstLine = resumeText
            .Split(
                ['\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .FirstOrDefault(line =>
                line.Length is >= 2 and <= 100);

        return string.IsNullOrWhiteSpace(firstLine)
            ? "Unknown Candidate"
            : firstLine;
    }

    private static List<string> ExtractLinesContaining(
        string text,
        IEnumerable<string> keywords)
    {
        return text
            .Split(
                ['\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line =>
                keywords.Any(keyword =>
                    line.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();
    }
}
namespace HirePathAI.Application.DTOs.Ats;

public class AtsAnalysisResponse
{
    public int Id { get; set; }

    public int JobId { get; set; }

    public string JobTitle { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string CandidateName { get; set; } = string.Empty;

    public string? CandidateEmail { get; set; }

    public string? CandidatePhone { get; set; }

    public string ResumeFileName { get; set; } = string.Empty;

    public List<string> ExtractedSkills { get; set; } = [];

    public List<string> MatchedSkills { get; set; } = [];

    public List<string> MissingSkills { get; set; } = [];

    public List<string> Education { get; set; } = [];

    public List<string> Experience { get; set; } = [];

    public List<string> Certifications { get; set; } = [];

    public double TotalExperienceYears { get; set; }

    public double SkillsScore { get; set; }

    public double ExperienceScore { get; set; }

    public double EducationScore { get; set; }

    public double CertificationScore { get; set; }

    public double AtsScore { get; set; }

    public double MatchPercentage { get; set; }

    public string Recommendation { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public DateTime ProcessedAt { get; set; }
}
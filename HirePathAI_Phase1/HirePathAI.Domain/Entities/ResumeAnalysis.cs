namespace HirePathAI.Domain.Entities;

public class ResumeAnalysis
{
    public int Id { get; set; }

    public string CandidateName { get; set; } = string.Empty;

    public string? CandidateEmail { get; set; }

    public string? CandidatePhone { get; set; }

    public string ResumeFileName { get; set; } = string.Empty;

    public string? ResumeFilePath { get; set; }

    public string ExtractedSkills { get; set; } = string.Empty;

    public string Education { get; set; } = string.Empty;

    public string Certifications { get; set; } = string.Empty;

    public double TotalExperienceYears { get; set; }

    public double SkillsScore { get; set; }

    public double ExperienceScore { get; set; }

    public double EducationScore { get; set; }

    public double CertificationScore { get; set; }

    public double AtsScore { get; set; }

    public double MatchPercentage { get; set; }

    public double MlConfidence { get; set; }

    public string Recommendation { get; set; } = string.Empty;

    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    public int JobId { get; set; }

    public Job Job { get; set; } = null!;
}
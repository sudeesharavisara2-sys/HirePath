namespace HirePathAI.Application.DTOs.Ats;

public class AiResumeAnalysisResult
{
    public string CandidateName { get; set; } = string.Empty;

    public string CandidateEmail { get; set; } = string.Empty;

    public string CandidatePhone { get; set; } = string.Empty;

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
}
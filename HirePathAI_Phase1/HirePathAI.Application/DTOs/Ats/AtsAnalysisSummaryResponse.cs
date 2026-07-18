namespace HirePathAI.Application.DTOs.Ats;

public class AtsAnalysisSummaryResponse
{
    public int Id { get; set; }

    public int JobId { get; set; }

    public string JobTitle { get; set; } = string.Empty;

    public string CandidateName { get; set; } = string.Empty;

    public string? CandidateEmail { get; set; }

    public string ResumeFileName { get; set; } = string.Empty;

    public double AtsScore { get; set; }

    public double MatchPercentage { get; set; }

    public string Recommendation { get; set; } = string.Empty;

    public DateTime ProcessedAt { get; set; }
}
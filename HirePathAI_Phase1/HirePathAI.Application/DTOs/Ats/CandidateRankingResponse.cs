namespace HirePathAI.Application.DTOs.Ats;

public class CandidateRankingResponse
{
    public int Rank { get; set; }

    public int AnalysisId { get; set; }

    public int JobId { get; set; }

    public string CandidateName { get; set; } = string.Empty;

    public string? CandidateEmail { get; set; }

    public double AtsScore { get; set; }

    public double MatchPercentage { get; set; }

    public string Recommendation { get; set; } = string.Empty;

    public List<string> MatchedSkills { get; set; } = [];

    public List<string> MissingSkills { get; set; } = [];

    public DateTime ProcessedAt { get; set; }
}
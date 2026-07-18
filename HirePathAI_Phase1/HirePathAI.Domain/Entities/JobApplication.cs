using HirePathAI.Domain.Enums;

namespace HirePathAI.Domain.Entities;

public class JobApplication
{
    public int Id { get; set; }

    public int CandidateId { get; set; }

    public Candidate Candidate { get; set; } = null!;

    public int JobId { get; set; }

    public Job Job { get; set; } = null!;

    public string? CoverLetter { get; set; }

    public ApplicationStatus Status { get; set; }
        = ApplicationStatus.Applied;

    public double? AtsScore { get; set; }

    public double? MatchPercentage { get; set; }

    public string? AiRecommendation { get; set; }

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
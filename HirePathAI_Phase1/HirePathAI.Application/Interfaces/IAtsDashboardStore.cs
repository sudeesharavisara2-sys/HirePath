namespace HirePathAI.Application.Interfaces;

public interface IAtsDashboardStore
{
    void Add(AtsCandidateSnapshot snapshot);
    AtsDashboardSummary GetSummary(int takeLatest = 20);
}

public sealed record AtsCandidateSnapshot(
    string CandidateName,
    string Decision,
    double OverallScore,
    double MlConfidence,
    DateTimeOffset Timestamp,
    int ExperienceCount,
    int SkillsCount);

public sealed record AtsDashboardSummary(
    int TotalProcessed,
    double AverageOverallScore,
    double SelectedPercent,
    double AverageMlConfidencePercent,
    int SelectedCount,
    int ConsiderCount,
    int RejectedCount,
    IReadOnlyList<AtsCandidateSnapshot> LatestCandidates,
    IReadOnlyList<AtsCandidateSnapshot> TopCandidates);

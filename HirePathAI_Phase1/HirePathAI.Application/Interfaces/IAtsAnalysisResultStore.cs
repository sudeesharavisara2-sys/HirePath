namespace HirePathAI.Application.Interfaces;

public interface IAtsAnalysisResultStore
{
    string Put(AtsAnalysisResult result);
    bool TryTake(string id, out AtsAnalysisResult result);
}

public sealed record AtsAnalysisResult(
    string ResumeText,
    bool Selected,
    string Decision,
    double ScoreConfidence,
    bool IsPdfUpload,
    string? ParsedName,
    string? ParsedEmail,
    string? ParsedPhone,
    IReadOnlyList<string> ParsedSkills,
    IReadOnlyList<string> ParsedEducation,
    IReadOnlyList<string> ParsedExperience,
    IReadOnlyList<string> ParsedCertifications,
    IReadOnlyList<string> ParsedProjects,
    double ScoreOverall,
    double ScoreSkills,
    double ScoreExperience,
    double ScoreEducation,
    double ScoreProject,
    double ScoreCertification,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Weaknesses,
    IReadOnlyList<string> MissingSkills,
    string Recommendation);


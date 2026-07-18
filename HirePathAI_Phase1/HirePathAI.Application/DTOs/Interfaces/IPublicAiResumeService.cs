using HirePathAI.Application.DTOs.Ats;

namespace HirePathAI.Application.Interfaces;

public interface IPublicAiResumeService
{
    Task<AiResumeAnalysisResult> AnalyzeResumeAsync(
        string resumeText,
        string jobDescription,
        CancellationToken cancellationToken = default);
}
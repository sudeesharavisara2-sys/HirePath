using HirePathAI.Application.DTOs.Ats;
using Microsoft.AspNetCore.Http;

namespace HirePathAI.Application.Interfaces;

public interface IAtsService
{
    Task<AtsAnalysisResponse> AnalyzeResumeAsync(
        int jobId,
        IFormFile resumeFile,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AtsAnalysisSummaryResponse>>
        GetAllAnalysesAsync(
            CancellationToken cancellationToken = default);

    Task<AtsAnalysisResponse?> GetAnalysisByIdAsync(
        int analysisId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CandidateRankingResponse>>
        GetCandidateRankingAsync(
            int jobId,
            CancellationToken cancellationToken = default);

    Task<bool> DeleteAnalysisAsync(
        int analysisId,
        CancellationToken cancellationToken = default);
}
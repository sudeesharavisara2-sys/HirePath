using Microsoft.AspNetCore.Http;

namespace HirePathAI.Application.DTOs.Ats;

public sealed class AnalyzeResumeRequest
{
    public int JobId { get; set; }

    public IFormFile ResumeFile { get; set; } = null!;
}
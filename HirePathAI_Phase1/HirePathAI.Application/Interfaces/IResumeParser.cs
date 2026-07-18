using HirePathAI.Domain.Entities;

namespace HirePathAI.Application.Interfaces
{
    public interface IResumeParser
    {
        ParsedResume Parse(string text, HirePathAI.Application.DTOs.JobRequirementDto jobContext);
    }
}

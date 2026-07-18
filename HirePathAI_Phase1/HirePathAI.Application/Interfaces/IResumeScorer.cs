using HirePathAI.Domain.Entities;
using HirePathAI.Domain.ValueObjects;

namespace HirePathAI.Application.Interfaces
{
    public interface IResumeScorer
    {
        ResumeScore Score(ParsedResume parsed, HirePathAI.Application.DTOs.JobRequirementDto job, double mlProbability);
    }
}

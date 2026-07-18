using HirePathAI.Application.DTOs;

namespace HirePathAI.Application.Interfaces
{
    public interface IJobStore
    {
        Task<IEnumerable<JobRequirementDto>> GetAllJobsAsync();
        Task<JobRequirementDto?> GetJobAsync(string id);
        Task CreateJobAsync(JobRequirementDto job);
        Task UpdateJobAsync(JobRequirementDto job);
        Task DeleteJobAsync(string id);
    }
}

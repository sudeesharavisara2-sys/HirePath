using HirePathAI.Application.DTOs;
using HirePathAI.Application.Interfaces;

namespace HirePathAI.Infrastructure.Repositories
{
    public class InMemoryJobStore : IJobStore
    {
        private readonly List<JobRequirementDto> _jobs = new();

        public InMemoryJobStore()
        {
            // Start empty as per Phase 1 requirement
        }

        public Task<IEnumerable<JobRequirementDto>> GetAllJobsAsync()
        {
            return Task.FromResult<IEnumerable<JobRequirementDto>>(_jobs.OrderByDescending(j => j.CreatedAt).ToList());
        }

        public Task<JobRequirementDto?> GetJobAsync(string id)
        {
            return Task.FromResult(_jobs.FirstOrDefault(j => j.Id == id));
        }

        public Task CreateJobAsync(JobRequirementDto job)
        {
            _jobs.Add(job);
            return Task.CompletedTask;
        }

        public Task UpdateJobAsync(JobRequirementDto job)
        {
            var existing = _jobs.FirstOrDefault(j => j.Id == job.Id);
            if (existing != null)
            {
                _jobs.Remove(existing);
                _jobs.Add(job);
            }
            return Task.CompletedTask;
        }

        public Task DeleteJobAsync(string id)
        {
            var existing = _jobs.FirstOrDefault(j => j.Id == id);
            if (existing != null)
            {
                _jobs.Remove(existing);
            }
            return Task.CompletedTask;
        }
    }
}

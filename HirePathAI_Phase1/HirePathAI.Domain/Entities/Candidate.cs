namespace HirePathAI.Domain.Entities;

public class Candidate
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public string? CurrentPosition { get; set; }

    public double TotalExperienceYears { get; set; }

    public string Skills { get; set; } = string.Empty;

    public string Education { get; set; } = string.Empty;

    public string? ResumeFilePath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<JobApplication> Applications { get; set; }
        = new List<JobApplication>();
}
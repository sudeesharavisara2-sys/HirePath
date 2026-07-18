namespace HirePathAI.Domain.Entities;

public class Job
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string RequiredSkills { get; set; } = string.Empty;

    public string PreferredSkills { get; set; } = string.Empty;

    public int MinimumExperienceYears { get; set; }

    public string MinimumEducation { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ClosingDate { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<ResumeAnalysis> ResumeAnalyses { get; set; }
        = new List<ResumeAnalysis>();
    public ICollection<JobApplication> Applications { get; set; }
    = new List<JobApplication>();
}
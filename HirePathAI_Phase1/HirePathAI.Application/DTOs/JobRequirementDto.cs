namespace HirePathAI.Application.DTOs
{
    public class JobRequirementDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CompanyName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        
        public List<string> RequiredSkills { get; set; } = new();
        public List<string> PreferredSkills { get; set; } = new();
        public int MinimumYearsExperience { get; set; }
        public string MinimumEducationLevel { get; set; } = "Bachelor";
        public List<string> RequiredCertifications { get; set; } = new();
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

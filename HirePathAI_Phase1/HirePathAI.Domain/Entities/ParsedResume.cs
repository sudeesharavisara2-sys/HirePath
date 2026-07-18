namespace HirePathAI.Domain.Entities
{
    public class ParsedResume
    {
        public string FullText { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public List<string> Skills { get; set; } = new();
        public List<EducationEntry> Education { get; set; } = new();
        public List<ExperienceEntry> Experience { get; set; } = new();
        public List<string> Certifications { get; set; } = new();
        public List<ProjectEntry> Projects { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
        public int TotalPages { get; set; }
        public bool IsScannedPdf { get; set; }
        public string RawExtractedText { get; set; } = string.Empty;
        public int TotalExperienceInMonths { get; set; }
    }

    public class EducationEntry
    {
        public string Degree { get; set; } = string.Empty;
        public string Institution { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
    }

    public class ExperienceEntry
    {
        public string JobTitle { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int YearsInMonths { get; set; }
    }

    public class ProjectEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Technologies { get; set; } = new();
    }
}

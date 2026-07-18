using System.ComponentModel.DataAnnotations;

namespace HirePathAI.Presentation.Models
{
    public class JobCreationViewModel
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string CompanyName { get; set; } = string.Empty;

        public string Department { get; set; } = string.Empty;

        public string RequiredSkillsCsv { get; set; } = string.Empty;
        
        public string OptionalSkillsCsv { get; set; } = string.Empty;

        public int MinimumYearsExperience { get; set; }

        public string MinimumEducationLevel { get; set; } = "Bachelor";
    }
}

namespace HirePathAI.Domain.ValueObjects
{
    public class ResumeScore
    {
        public double OverallScore { get; set; }
        public double SkillsScore { get; set; }
        public double ExperienceScore { get; set; }
        public double EducationScore { get; set; }
        public double ProjectScore { get; set; }
        public double CertificationScore { get; set; }
        public double MLConfidence { get; set; }
        public string Decision { get; set; } = string.Empty;
        public List<string> Strengths { get; set; } = new();
        public List<string> Weaknesses { get; set; } = new();
        public List<string> MissingSkills { get; set; } = new();
        public string Recommendation { get; set; } = string.Empty;
    }
}

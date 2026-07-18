using HirePathAI.Application.Interfaces;
using HirePathAI.Application.DTOs;
using HirePathAI.Domain.Entities;
using HirePathAI.Domain.ValueObjects;

namespace HirePathAI.Application.Services
{
    public class ScoringService : IResumeScorer
    {
        public ResumeScore Score(ParsedResume parsed, JobRequirementDto job, double mlProbability)
        {
            var score = new ResumeScore
            {
                MLConfidence = mlProbability
            };

            // Phase 3: Dynamic Matching Engine
            score.SkillsScore = CalculateSkillsScore(parsed.Skills, job);
            score.ExperienceScore = CalculateExperienceScore(parsed, job);
            score.EducationScore = CalculateEducationScore(parsed.Education, job);
            score.ProjectScore = CalculateProjectScore(parsed.Projects); // Keep a minor internal score
            score.CertificationScore = CalculateCertificationScore(parsed.Certifications, job);

            // Phase 4: ATS Score Refactor
            // Skill Match = 40%, Experience = 30%, Education = 15%, ML.NET = 15%
            var weightedScore = (score.SkillsScore * 0.40) + 
                                (score.ExperienceScore * 0.30) + 
                                (score.EducationScore * 0.15) + 
                                (mlProbability * 100 * 0.15);

            score.OverallScore = Math.Clamp(weightedScore, 0, 100);

            // Phase 5: Decision Engine
            score.Decision = GetDecision(score.OverallScore);

            score.MissingSkills = IdentifyMissingSkills(parsed.Skills, job);
            score.Strengths = IdentifyStrengths(parsed, score, job);
            score.Weaknesses = IdentifyWeaknesses(parsed, score, job);
            score.Recommendation = GetRecommendation(score);

            return score;
        }

        private double CalculateSkillsScore(List<string> candidateSkills, JobRequirementDto job)
        {
            if (job.RequiredSkills.Count == 0 && job.PreferredSkills.Count == 0)
                return 100; // No requirements = perfect match

            var candidateLower = candidateSkills.Select(s => s.ToLower()).ToHashSet();
            
            double requiredScore = 0;
            if (job.RequiredSkills.Count > 0)
            {
                double requiredMatched = job.RequiredSkills.Count(req => candidateLower.Any(c => c.Contains(req.ToLower()) || req.ToLower().Contains(c)));
                requiredScore = (requiredMatched / job.RequiredSkills.Count) * 80.0; // 80% weight for required skills
            }
            else
            {
                requiredScore = 80.0;
            }

            double preferredScore = 0;
            if (job.PreferredSkills.Count > 0)
            {
                double preferredMatched = job.PreferredSkills.Count(pref => candidateLower.Any(c => c.Contains(pref.ToLower()) || pref.ToLower().Contains(c)));
                preferredScore = (preferredMatched / job.PreferredSkills.Count) * 20.0; // 20% weight for preferred skills
            }
            else
            {
                preferredScore = 20.0;
            }

            return Math.Clamp(requiredScore + preferredScore, 0, 100);
        }

        private double CalculateExperienceScore(ParsedResume parsed, JobRequirementDto job)
        {
            var totalMonths = Math.Max(parsed.TotalExperienceInMonths, parsed.Experience.Sum(e => e.YearsInMonths));
            var totalYears = totalMonths / 12.0;

            if (job.MinimumYearsExperience <= 0)
                return 100; // Entry level

            double score = (totalYears / job.MinimumYearsExperience) * 100.0;
            return Math.Clamp(score, 0, 100);
        }

        private double CalculateEducationScore(List<EducationEntry> education, JobRequirementDto job)
        {
            var requiredLevel = job.MinimumEducationLevel?.ToLower() ?? "none";
            if (requiredLevel == "none") return 100;

            if (education.Count == 0) return 0;

            var degreeScores = new Dictionary<string, int>
            {
                { "none", 0 },
                { "associate", 1 },
                { "bachelor", 2 },
                { "master", 3 },
                { "phd", 4 }
            };

            int reqScore = degreeScores.ContainsKey(requiredLevel) ? degreeScores[requiredLevel] : 0;
            int maxCandidateScore = 0;

            foreach (var edu in education)
            {
                var deg = edu.Degree.ToLower();
                int score = 0;
                if (deg.Contains("phd") || deg.Contains("doctorate") || deg.Contains("ph.d")) score = 4;
                else if (deg.Contains("master") || deg.Contains("mba") || deg.Contains("m.tech") || deg.Contains("m.sc") || deg.Contains("mca")) score = 3;
                else if (deg.Contains("bachelor") || deg.Contains("b.tech") || deg.Contains("b.sc") || deg.Contains("b.a") || deg.Contains("b.com") || deg.Contains("b.e.") || deg.Contains("bca") || deg.Contains("bba")) score = 2;
                else if (deg.Contains("associate") || deg.Contains("diploma")) score = 1;
                
                if (score > maxCandidateScore) maxCandidateScore = score;
            }

            if (maxCandidateScore >= reqScore)
                return 100;
            
            return Math.Max(0, (maxCandidateScore / (double)reqScore) * 100);
        }

        private double CalculateProjectScore(List<ProjectEntry> projects)
        {
            if (projects.Count == 0) return 0;
            var countScore = Math.Min(projects.Count * 25, 100);
            return countScore;
        }

        private double CalculateCertificationScore(List<string> certifications, JobRequirementDto job)
        {
            if (job.RequiredCertifications.Count == 0) return 100;
            if (certifications.Count == 0) return 0;

            var certsLower = certifications.Select(c => c.ToLower()).ToList();
            double matched = job.RequiredCertifications.Count(req => certsLower.Any(c => c.Contains(req.ToLower()) || req.ToLower().Contains(c)));
            
            return (matched / job.RequiredCertifications.Count) * 100.0;
        }

        private List<string> IdentifyStrengths(ParsedResume parsed, ResumeScore score, JobRequirementDto job)
        {
            var strengths = new List<string>();

            if (score.SkillsScore >= 80)
                strengths.Add($"Strong alignment with {job.Title} skill requirements");
            
            if (score.ExperienceScore >= 100 && job.MinimumYearsExperience > 0)
                strengths.Add($"Meets or exceeds the {job.MinimumYearsExperience} years experience requirement");

            if (score.EducationScore >= 100 && job.MinimumEducationLevel != "None")
                strengths.Add($"Meets the required educational level ({job.MinimumEducationLevel})");

            if (parsed.Projects.Count > 0)
                strengths.Add($"Has practical project experience ({parsed.Projects.Count} projects)");

            return strengths;
        }

        private List<string> IdentifyWeaknesses(ParsedResume parsed, ResumeScore score, JobRequirementDto job)
        {
            var weaknesses = new List<string>();

            if (string.IsNullOrEmpty(parsed.Name))
                weaknesses.Add("Could not extract candidate name");

            if (string.IsNullOrEmpty(parsed.Email))
                weaknesses.Add("No email address found");

            if (score.SkillsScore < 50)
                weaknesses.Add($"Lacks several core skills required for {job.Title}");

            if (score.ExperienceScore < 100 && job.MinimumYearsExperience > 0)
            {
                if (parsed.TotalExperienceInMonths == 0)
                    weaknesses.Add("Does not meet minimum experience requirement (0 years detected)");
                else
                    weaknesses.Add($"Falls short of the {job.MinimumYearsExperience} years experience requirement ({(parsed.TotalExperienceInMonths/12.0):F1} years detected)");
            }

            if (score.EducationScore < 100 && job.MinimumEducationLevel != "None")
                weaknesses.Add($"Does not meet the minimum educational level of {job.MinimumEducationLevel}");

            return weaknesses;
        }

        private List<string> IdentifyMissingSkills(List<string> existingSkills, JobRequirementDto job)
        {
            var existingLower = existingSkills.Select(s => s.ToLower()).ToHashSet();
            var missing = new List<string>();

            foreach (var req in job.RequiredSkills)
            {
                if (!existingLower.Any(e => e.Contains(req.ToLower()) || req.ToLower().Contains(e)))
                {
                    missing.Add(req);
                }
            }

            return missing;
        }

        private string GetDecision(double overallScore)
        {
            if (overallScore >= 75)
                return "Selected";
            else if (overallScore >= 50)
                return "Consider";
            else
                return "Rejected";
        }

        private string GetRecommendation(ResumeScore score)
        {
            if (score.Decision == "Selected")
                return "Strong match for the job requirements. Proceed to interview.";
            else if (score.Decision == "Consider")
                return "Partial match. Might require additional technical screening.";
            else
                return "Does not meet the requirements for this specific role.";
        }
    }
}

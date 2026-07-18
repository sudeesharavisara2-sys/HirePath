using System.Text.RegularExpressions;
using HirePathAI.Application.Interfaces;
using HirePathAI.Domain.Entities;

namespace HirePathAI.Infrastructure.AI.Services
{
    public class ResumeParserService : IResumeParser
    {


        public ParsedResume Parse(string text, HirePathAI.Application.DTOs.JobRequirementDto jobContext)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new ParsedResume { FullText = text ?? "" };

            var resume = new ParsedResume
            {
                FullText = text,
                RawExtractedText = text
            };

            resume.Email = ExtractEmail(text);
            resume.Phone = ExtractPhone(text);
            resume.Name = ExtractName(text, resume.Email, resume.Phone);
            resume.Skills = ExtractSkills(text, jobContext).Distinct().ToList();
            resume.Education = ExtractEducation(text);
            resume.Experience = ExtractExperience(text);
            resume.Certifications = ExtractCertifications(text);
            resume.Projects = ExtractProjects(text, jobContext);
            resume.Summary = ExtractSummary(text);
            
            var experienceSection = ExtractSection(text, new[] { "experience", "work experience", "employment", "work history", "professional experience", "career", "internship", "internships" });
            if (string.IsNullOrEmpty(experienceSection)) experienceSection = text;
            resume.TotalExperienceInMonths = CalculateTotalExperienceMonths(experienceSection);

            return resume;
        }

        private string ExtractEmail(string text)
        {
            var match = Regex.Match(text, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.IgnoreCase);
            return match.Success ? match.Value : string.Empty;
        }

        private string ExtractPhone(string text)
        {
            var patterns = new[]
            {
                @"\+?\d{1,3}[-.\s]?\(?\d{2,4}\)?[-.\s]?\d{3,4}[-.\s]?\d{3,4}",
                @"\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}",
                @"\d{3}[-.\s]\d{3}[-.\s]\d{4}"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern);
                if (match.Success)
                    return match.Value.Trim();
            }
            return string.Empty;
        }

        private string ExtractName(string text, string email, string phone)
        {
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim().TrimEnd(','))
                .Where(l => l.Length > 0)
                .ToList();

            var sectionIndicators = new[] { "summary:", "objective:", "experience:", "skills:",
                "education:", "profile:", "contact:", "professional summary:", "career objective:",
                "technical skills:", "work experience:", "employment:", "certifications:",
                "projects:", "publications:", "languages:", "references:", "internship:",
                "achievements:", "awards:", "honors:" };

            var nameCandidates = new List<(string Name, int Index, int WordCount, int Score)>();

            var knownNonNames = new[] { "oracle", "microsoft", "amazon", "google", "azure", "aws", "docker",
                "kubernetes", "jenkins", "terraform", "python", "javascript", "typescript", "java",
                "html", "css", "react", "angular", "vue", "node", "express", "mongodb", "mysql",
                "postgresql", "redis", "git", "github", "gitlab", "jira", "confluence",
                "junior", "senior", "principal", "staff", "lead", "head", "director", "manager",
                "engineer", "developer", "software", "intern", "trainee", "associate", "analyst", "architect",
                "consultant", "specialist", "coordinator", "administrator",
                "professional", "summary", "technical", "skills", "work", "experience",
                "education", "certifications", "projects", "languages", "tools",
                "database", "cloud", "frameworks", "other", "frontend", "backend",
                "achievements", "awards", "publications", "references", "languages",
                "linkedin", "github", "portfolio", "website",
                "massachusetts", "university", "college", "institute", "school", "academy",
                "bachelor", "master", "ph.d", "phd", "doctorate", "diploma",
                "board", "membership", "memberships", "open", "source", "executive",
                "summary", "patent", "patents", "publication", "publications",
                "speaker", "conference", "conferences", "technical", "advisory" };

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var lower = line.ToLower().TrimEnd(':');

                if (string.IsNullOrEmpty(line)) continue;
                if (line.Length > 60) continue;
                if (line.Contains("@")) continue;
                if (!string.IsNullOrEmpty(phone) && line.Contains(phone)) continue;
                if (sectionIndicators.Any(s => lower.StartsWith(s) || lower.Contains(s.Replace(":", "")))) continue;
                if (Regex.IsMatch(line, @"^\d")) continue;
                if (Regex.IsMatch(line, @"^(www|http|@)")) continue;
                if (Regex.IsMatch(line, @"^\s*[A-Z][a-z]+(?:,\s*[A-Z][a-z]+)+$")) continue;

                if (knownNonNames.Any(k => lower.StartsWith(k))) continue;
                if (lower.Contains("(") || lower.Contains("ph.d") || lower.Contains("phd ")) continue;

                var namePart = line.Split(new[] { " - ", " – ", " | ", "  ", " / " }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                if (namePart.Contains(",")) continue;
                if (Regex.IsMatch(namePart, @"[\(\)\[\]{}]")) continue;

                var wds = namePart.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (wds.Length < 2 || wds.Length > 5) continue;

                var first = wds[0].TrimEnd('.', ',');
                var last = wds[^1].TrimEnd('.', ',');

                bool allLetters = wds.All(w => w.All(c => char.IsLetter(c) || c == '.' || c == '\'' || c == '-'));
                bool startsWithUpper = char.IsUpper(first[0]) && char.IsUpper(last[0]);

                if (!startsWithUpper && !allLetters) continue;

                int wordScore = 0;
                if (wds.Length == 2) wordScore = 10;
                else if (wds.Length == 3) wordScore = 7;
                else wordScore = 3;

                if (Regex.IsMatch(namePart, @"^[A-Z][a-z]+(-[A-Z][a-z]+)?\s+[A-Z][a-z]+$")) wordScore += 5;

                if (startsWithUpper)
                {
                    nameCandidates.Add((namePart, i, wds.Length, wordScore));
                }
                else if (allLetters)
                {
                    var caps = wds.Count(w => w.Length > 0 && char.IsUpper(w[0]));
                    if (caps >= wds.Length - 1)
                        nameCandidates.Add((namePart, i, wds.Length, wordScore));
                }
            }

            if (nameCandidates.Count > 0)
            {
                var best = nameCandidates
                    .OrderByDescending(c => c.Score)
                    .ThenBy(c => c.WordCount)
                    .ThenBy(c => Math.Abs(c.Index - lines.Count / 2))
                    .First();
                var candidateName = best.Name.TrimEnd(',', '.');
                var words = candidateName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length >= 2 && words.Length <= 4 && words.All(w => w.TrimEnd('.', ',').All(c => char.IsLetter(c)) || w.Length == 1))
                    return candidateName;
            }

            return string.Empty;
        }

        private bool IsLikelyName(string text)
        {
            if (Regex.IsMatch(text, @"^\d")) return false;
            if (Regex.IsMatch(text, @"^(www|http|@)")) return false;
            if (Regex.IsMatch(text, @"[\[\]{}|\\^$%#&*±§]")) return false;
            return true;
        }

        private List<string> ExtractSkills(string text, HirePathAI.Application.DTOs.JobRequirementDto jobContext)
        {
            var foundSkills = new List<string>();
            var lowerText = text.ToLower();

            // Extract skills based ONLY on the dynamic Job Requirements (removing hardcoded arrays)
            var dynamicTechSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (jobContext != null)
            {
                foreach (var s in jobContext.RequiredSkills) dynamicTechSkills.Add(s);
                foreach (var s in jobContext.PreferredSkills) dynamicTechSkills.Add(s);
            }

            foreach (var skill in dynamicTechSkills)
            {
                var escaped = Regex.Escape(skill);
                if (Regex.IsMatch(lowerText, @"\b" + escaped + @"\b"))
                    foundSkills.Add(skill);
            }

            var explicitSection = ExtractSection(text, new[] { "technical skills", "skills", "core competencies", "key skills", "technologies" });
            if (!string.IsNullOrEmpty(explicitSection))
            {
                var skillLines = explicitSection.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .SelectMany(l => l.Split(new[] { ',', '|', '-', '•', '*', '/' }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(l => l.Trim().TrimStart('●', '○', '▪', '▸', '›'))
                    .Where(l => l.Length > 1 && l.Length < 80);

                foreach (var line in skillLines)
                {
                    if (!foundSkills.Contains(line, StringComparer.OrdinalIgnoreCase))
                    {
                        foundSkills.Add(line); // We add all explicitly declared skills to avoid dropping them
                    }
                }
            }

            return foundSkills;
        }

        private List<EducationEntry> ExtractEducation(string text)
        {
            var entries = new List<EducationEntry>();
            var section = ExtractSection(text, new[] { "education", "academic background", "qualifications", "academic qualifications", "formal education" });

            if (string.IsNullOrEmpty(section))
                section = text;

            var degreePatterns = new[]
            {
                @"\b(bachelor|master|phd|doctorate|associate|mba|b\.?[ae]|m\.?[ae]|ph\.?d)\b",
                @"\b(?:B\.?Tech|M\.?Tech|B\.?Sc|M\.?Sc|B\.?A|M\.?A|B\.?Com|M\.?Com|BBA|MBA|BCA|MCA|LLB|LLM|B\.?E|M\.?E)\b",
                @"\b(bachelor(?:'s)?\s+of\s+\w+|master(?:'s)?\s+of\s+\w+)\b",
                @"\b(?:PhD|Ph\.D\.?|Doctorate)\s+(?:in\s+)?",
                @"\bdiploma\b"
            };

            var lines = section.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .ToList();

            EducationEntry current = new();
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var lower = line.ToLower();
                bool hasDegree = degreePatterns.Any(p => Regex.IsMatch(line, p, RegexOptions.IgnoreCase));

                if (hasDegree)
                {
                    if (!string.IsNullOrEmpty(current.Degree))
                        entries.Add(current);

                    current = new EducationEntry { Degree = line };
                    var yearMatch = Regex.Match(line, @"(?:19|20)\d{2}");
                    if (yearMatch.Success) current.Year = yearMatch.Value;

                    foreach (var inst in new[] { "university", "college", "institute", "school", "academy" })
                    {
                        if (lower.Contains(inst))
                        {
                            current.Institution = line;
                            break;
                        }
                    }

                    var fieldMatch = Regex.Match(line, @"(?:in|of|–)\s+(.+?)(?:\s*(?:-|–|\d{4}|$))", RegexOptions.IgnoreCase);
                    if (fieldMatch.Success) current.Field = fieldMatch.Groups[1].Value.Trim();
                }
                else if (Regex.IsMatch(line, @"(?:19|20)\d{2}", RegexOptions.IgnoreCase))
                {
                    current.Year = Regex.Match(line, @"(?:19|20)\d{2}").Value;
                    if (string.IsNullOrEmpty(current.Institution))
                        current.Institution = line;
                }
                else if (!string.IsNullOrEmpty(current.Degree))
                {
                    var instPattern = @"(?:university|college|institute|academy|school)\s+of\s+\w+|\w+\s+(?:university|college|institute|academy)";
                    if (Regex.IsMatch(line, instPattern, RegexOptions.IgnoreCase))
                        current.Institution = line;
                }
            }

            if (!string.IsNullOrEmpty(current.Degree))
                entries.Add(current);

            if (entries.Count == 0)
            {
                var allLines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .Where(l => l.Length > 0)
                    .ToList();

                var eduKeywordLines = allLines.Where(l =>
                {
                    var lower = l.ToLower();
                    return lower.Contains("b.tech") || lower.Contains("b.e.") || lower.Contains("b.e ") ||
                           lower.Contains("m.tech") || lower.Contains("b.sc") || lower.Contains("m.sc") ||
                           lower.Contains("bachelor") || lower.Contains("master") || lower.Contains("phd") ||
                           lower.Contains("bca") || lower.Contains("mca") || lower.Contains("bba") ||
                           lower.Contains("mba") || lower.Contains("diploma") ||
                           (lower.Contains("university") && Regex.IsMatch(l, @"\b(?:19|20)\d{2}\b"));
                }).ToList();

                foreach (var eduLine in eduKeywordLines)
                {
                    var entry = new EducationEntry { Degree = eduLine };
                    var yearMatch = Regex.Match(eduLine, @"(?:19|20)\d{2}");
                    if (yearMatch.Success) entry.Year = yearMatch.Value;
                    foreach (var inst in new[] { "university", "college", "institute", "school", "academy" })
                    {
                        if (eduLine.ToLower().Contains(inst))
                        {
                            entry.Institution = eduLine;
                            break;
                        }
                    }
                    entries.Add(entry);
                }
            }

            return entries;
        }

        private List<ExperienceEntry> ExtractExperience(string text)
        {
            var entries = new List<ExperienceEntry>();
            var allLines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .ToList();

            var expHeaders = new[] { "experience", "work experience", "employment", "work history", "professional experience", "career", "career history" };
            var stopHeaders = new[] { "projects", "education", "certifications", "skills", "languages", "references", "summary", "profile", "objective", "technical skills", "achievements", "awards", "volunteer" };

            var datePattern = @"(?:\b(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s*|\d{1,2}[/-])?(?:19|20)\d{2}\s*(?:-|–|to)\s*(?:(?:\b(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s*|\d{1,2}[/-])?(?:19|20)\d{2}|present|current|now)";
            var titlePatterns = new[] { "engineer", "developer", "manager", "lead", "architect", "analyst", "designer",
                "intern", "consultant", "specialist", "director", "head", "officer", "associate", "trainee",
                "administrator", "coordinator", "supervisor", "principal", "programmer", "scientist", "writer" };

            bool hasExpHeader = allLines.Any(l => expHeaders.Any(h => l.ToLower().TrimEnd(':', '.').StartsWith(h) && l.Length < 35));
            bool inExpSection = !hasExpHeader; 
            
            ExperienceEntry current = null;

            for (int i = 0; i < allLines.Count; i++)
            {
                var line = allLines[i];
                var lower = line.ToLower().TrimEnd(':', '.');

                if (expHeaders.Any(h => lower.StartsWith(h) && line.Length < 35))
                {
                    inExpSection = true;
                    System.Diagnostics.Debug.WriteLine($"[Parser] Entered Experience Section: {line}");
                    continue;
                }
                
                if (stopHeaders.Any(h => lower.StartsWith(h) && line.Length < 35))
                {
                    if (inExpSection) System.Diagnostics.Debug.WriteLine($"[Parser] Exited Experience Section at: {line}");
                    inExpSection = false; 
                    continue;
                }

                if (!inExpSection || (i < 5 && !hasExpHeader))
                {
                    continue;
                }

                if (lower.Replace(":", "") == "responsibilities" || lower.Replace(":", "") == "achievements" || lower.Replace(":", "") == "key responsibilities" || lower == "projects:")
                    continue;

                bool isBullet = line.StartsWith("•") || line.StartsWith("-") || line.StartsWith("*");
                
                if (isBullet)
                {
                    if (current != null) current.Description += "\n" + line;
                    continue;
                }

                var hasDate = Regex.IsMatch(line, datePattern, RegexOptions.IgnoreCase) || Regex.IsMatch(line, @"\b(?:19|20)\d{2}\s*[-–]\s*(?:19|20)\d{2}\b");
                var hasTitle = titlePatterns.Any(t => Regex.IsMatch(lower, $@"\b{t}\b"));
                var isShortLine = !hasDate && !hasTitle && line.Split(' ').Length <= 5;

                if (hasDate || hasTitle || isShortLine)
                {
                    bool startingNew = false;
                    if (current != null)
                    {
                        if (hasDate && !string.IsNullOrEmpty(current.Duration)) startingNew = true;
                        if (hasTitle && !string.IsNullOrEmpty(current.JobTitle)) startingNew = true;
                        if (isShortLine && !string.IsNullOrEmpty(current.Company) && !string.IsNullOrEmpty(current.JobTitle) && !string.IsNullOrEmpty(current.Duration)) startingNew = true;
                    }

                    if (startingNew)
                    {
                        if (!string.IsNullOrEmpty(current.JobTitle) && !string.IsNullOrEmpty(current.Duration))
                        {
                            System.Diagnostics.Debug.WriteLine($"[Parser] Valid Record -> Title: {current.JobTitle} | Company: {current.Company} | Date: {current.Duration} | Confidence: High");
                            entries.Add(current);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[Parser] Invalid Record Discarded -> Title: {current.JobTitle} | Date: {current.Duration}");
                        }
                        current = new ExperienceEntry();
                    }

                    if (current == null) current = new ExperienceEntry();

                    if (hasDate)
                    {
                        var match = Regex.Match(line, datePattern, RegexOptions.IgnoreCase);
                        if (match.Success) current.Duration = match.Value;
                        else current.Duration = Regex.Match(line, @"\b(?:19|20)\d{2}\s*[-–]\s*(?:19|20)\d{2}\b").Value;

                        var remaining = line.Replace(current.Duration, "").Trim(new[] { ' ', '|', '-', '–', ',' });
                        if (!string.IsNullOrWhiteSpace(remaining))
                        {
                            var parts = remaining.Split(new[] { '|', '-', '–', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray();
                            if (parts.Length >= 2)
                            {
                                if (titlePatterns.Any(t => parts[0].ToLower().Contains(t))) { current.JobTitle = parts[0]; current.Company = parts[1]; }
                                else { current.Company = parts[0]; current.JobTitle = parts[1]; }
                            }
                            else if (parts.Length == 1)
                            {
                                if (titlePatterns.Any(t => parts[0].ToLower().Contains(t))) current.JobTitle = parts[0];
                                else if (string.IsNullOrEmpty(current.Company)) current.Company = parts[0];
                            }
                        }
                    }
                    else if (hasTitle)
                    {
                        var parts = line.Split(new[] { '|', '-', '–', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray();
                        if (parts.Length >= 2)
                        {
                            if (titlePatterns.Any(t => parts[0].ToLower().Contains(t))) { current.JobTitle = parts[0]; current.Company = string.Join(" ", parts.Skip(1)); }
                            else { current.Company = parts[0]; current.JobTitle = string.Join(" ", parts.Skip(1)); }
                        }
                        else
                        {
                            current.JobTitle = line;
                        }
                    }
                    else if (isShortLine && string.IsNullOrEmpty(current.Company))
                    {
                        current.Company = line;
                    }
                    else
                    {
                        current.Description += "\n" + line;
                    }
                }
                else
                {
                    if (current != null) current.Description += "\n" + line;
                }
            }

            if (current != null)
            {
                if (!string.IsNullOrEmpty(current.JobTitle) && !string.IsNullOrEmpty(current.Duration))
                {
                    System.Diagnostics.Debug.WriteLine($"[Parser] Valid Record -> Title: {current.JobTitle} | Company: {current.Company} | Date: {current.Duration} | Confidence: High");
                    entries.Add(current);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[Parser] Invalid Record Discarded -> Title: {current.JobTitle} | Date: {current.Duration}");
                }
            }

            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.Company)) entry.Company = "Company Name Not Extracted";
                
                entry.YearsInMonths = CalculateMonths(entry.Duration);
                
                if (entry.YearsInMonths > 0)
                {
                    int years = entry.YearsInMonths / 12;
                    int months = entry.YearsInMonths % 12;
                    var parts = new List<string>();
                    if (years > 0) parts.Add($"{years} Year{(years > 1 ? "s" : "")}");
                    if (months > 0) parts.Add($"{months} Month{(months > 1 ? "s" : "")}");
                    if (parts.Count > 0)
                    {
                        entry.Duration = $"{entry.Duration} <br/><span style='color:#10B981;font-weight:600;'>{string.Join(" ", parts)}</span>";
                    }
                }
            }

            return entries;
        }

        private List<string> ExtractCertifications(string text)
        {
            var certs = new List<string>();
            var section = ExtractSection(text, new[] { "certifications", "certificates", "licenses", "professional certifications", "certification" });

            if (!string.IsNullOrEmpty(section))
            {
                var lines = section.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim().TrimStart('•', '●', '○', '▪', '-', '*', '→'))
                    .Where(l => l.Length > 3)
                    .ToList();

                foreach (var line in lines)
                {
                    if (line.Length < 100 && !line.ToLower().StartsWith("certifications"))
                        certs.Add(line);
                }
            }

            if (certs.Count == 0)
            {
                var certKeywords = new[] { "certified", "certification", "certificate", "licensed", "license" };
                var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim().TrimStart('•', '●', '○', '▪', '-', '*', '→'))
                    .Where(l => l.Length > 5 && l.Length < 120)
                    .ToList();

                foreach (var line in lines)
                {
                    var lower = line.ToLower();
                    if (certKeywords.Any(k => lower.Contains(k)) && !lower.StartsWith("certifications"))
                    {
                        certs.Add(line);
                        if (certs.Count >= 10) break;
                    }
                }
            }

            return certs;
        }

        private List<ProjectEntry> ExtractProjects(string text, HirePathAI.Application.DTOs.JobRequirementDto jobContext)
        {
            var projects = new List<ProjectEntry>();
            var section = ExtractSection(text, new[] { "projects", "project experience", "academic projects", "personal projects", "key projects" });

            if (string.IsNullOrEmpty(section))
                return projects;

            var dynamicTechSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (jobContext != null)
            {
                foreach (var s in jobContext.RequiredSkills) dynamicTechSkills.Add(s);
                foreach (var s in jobContext.PreferredSkills) dynamicTechSkills.Add(s);
            }

            var lines = section.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .ToList();

            foreach (var line in lines)
            {
                var lower = line.ToLower();
                if (lower.StartsWith("projects") || lower.StartsWith("project experience"))
                    continue;

                var techs = dynamicTechSkills.Where(s =>
                    Regex.IsMatch(line, @"\b" + Regex.Escape(s) + @"\b", RegexOptions.IgnoreCase))
                    .ToList();

                if (techs.Count > 0 || line.Length > 20)
                {
                    var name = line.Split(',', ';', '-', '–', ':').FirstOrDefault()?.Trim() ?? line;
                    if (name.Length > 60) name = name[..60] + "...";

                    projects.Add(new ProjectEntry
                    {
                        Name = name,
                        Description = line.Length > 60 ? line : "",
                        Technologies = techs
                    });
                }
            }

            return projects;
        }

        private string ExtractSummary(string text)
        {
            var section = ExtractSection(text, new[] { "summary", "professional summary", "career summary", "profile", "objective", "career objective" });

            if (!string.IsNullOrEmpty(section))
            {
                var lines = section.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .Where(l => l.Length > 10 && !l.ToLower().StartsWith("summary"))
                    .ToList();

                if (lines.Count > 0)
                    return string.Join(" ", lines);
            }

            var firstLines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => l.Length > 20)
                .Take(3);

            return string.Join(" ", firstLines);
        }

        private string ExtractSection(string text, string[] sectionHeaders)
        {
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select((l, i) => new { Line = l.Trim(), Index = i })
                .ToList();

            int? startIndex = null;
            string? matchedHeader = null;

            foreach (var item in lines)
            {
                var lower = item.Line.ToLower().TrimEnd(':', '.');
                if (sectionHeaders.Any(h => lower.StartsWith(h) || lower.Contains(h)))
                {
                    if (startIndex == null)
                    {
                        startIndex = item.Index;
                        matchedHeader = item.Line;
                    }
                }
            }

            if (startIndex == null) return string.Empty;

            var currentHeader = matchedHeader;
            var skipHeaders = new[] { "education", "experience", "skills", "technical skills", "projects",
                "certifications", "summary", "professional summary", "objective", "career objective",
                "work experience", "employment", "publications", "languages", "references",
                "achievements", "awards", "honors", "interests", "volunteer", "additional",
                "internship", "internships" };

            var sectionLines = new List<string>();

            var headerLine = lines.FirstOrDefault(l => l.Index == startIndex);
            if (headerLine != null)
            {
                var colonIdx = headerLine.Line.IndexOf(':');
                if (colonIdx > 0)
                {
                    var afterColon = headerLine.Line[(colonIdx + 1)..].Trim();
                    if (!string.IsNullOrEmpty(afterColon))
                        sectionLines.Add(afterColon);
                }
            }

            bool inSection = false;

            foreach (var item in lines)
            {
                if (item.Index == startIndex)
                {
                    inSection = true;
                    continue;
                }

                if (inSection && !string.IsNullOrEmpty(item.Line))
                {
                    var lower = item.Line.ToLower().TrimEnd(':', '.');
                    bool isNextSection = skipHeaders.Any(h => lower.StartsWith(h)) &&
                                         !sectionHeaders.Any(h => lower.StartsWith(h)) &&
                                         item.Line.Length < 40;

                    if (isNextSection)
                        break;

                    sectionLines.Add(item.Line);
                }
            }

            return string.Join("\n", sectionLines);
        }

        private string ExtractDuration(string line)
        {
            var match = Regex.Match(line, @"((?:(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+|\d{1,2}[/-])?(?:19|20)\d{2}\s*(?:-|–|to)\s*(?:(?:(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+|\d{1,2}[/-])?(?:19|20)\d{2}|present|current|now))", RegexOptions.IgnoreCase);
            return match.Success ? match.Value : string.Empty;
        }

        private int CalculateMonths(string duration)
        {
            if (string.IsNullOrEmpty(duration)) return 0;
            
            var datePattern = @"(?:(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+|\d{1,2}[/-])?((?:19|20)\d{2})";
            var matches = Regex.Matches(duration, datePattern, RegexOptions.IgnoreCase);
            
            var years = matches.Select(m => int.Parse(m.Groups[1].Value)).ToList();
            if (years.Count == 0)
            {
                // Try just year
                matches = Regex.Matches(duration, @"(?:19|20)\d{2}");
                years = matches.Select(m => int.Parse(m.Value)).ToList();
            }

            if (years.Count < 1) return 0;
            if (years.Count == 1)
            {
                var lower = duration.ToLower();
                if (lower.Contains("present") || lower.Contains("current") || lower.Contains("now"))
                    return Math.Max(0, (DateTime.Now.Year - years[0]) * 12);
                return 12;
            }

            return Math.Max(0, (years[^1] - years[0]) * 12);
        }

        public int CalculateTotalExperienceMonths(string experienceSection)
        {
            if (string.IsNullOrWhiteSpace(experienceSection)) return 0;

            var datePattern = @"((?:(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+|\d{1,2}[/-])?(?:19|20)\d{2})\s*(?:-|–|to)\s*((?:(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+|\d{1,2}[/-])?(?:19|20)\d{2}|present|current|now)";
            var matches = Regex.Matches(experienceSection, datePattern, RegexOptions.IgnoreCase);
            
            var intervals = new List<(DateTime Start, DateTime End)>();

            foreach (Match match in matches)
            {
                var startStr = match.Groups[1].Value;
                var endStr = match.Groups[2].Value.ToLower();

                DateTime start = ParseDate(startStr, true);
                DateTime end = (endStr.Contains("present") || endStr.Contains("current") || endStr.Contains("now")) 
                    ? DateTime.Now 
                    : ParseDate(endStr, false);

                if (start < end && start.Year > 1950 && start <= DateTime.Now)
                {
                    intervals.Add((start, end));
                }
            }

            // Also try to find isolated single years if no ranges found
            if (intervals.Count == 0)
            {
                var singleYearPattern = @"\b(?:19|20)\d{2}\b";
                var singleMatches = Regex.Matches(experienceSection, singleYearPattern);
                var years = singleMatches.Select(m => int.Parse(m.Value)).Where(y => y > 1950 && y <= DateTime.Now.Year).ToList();
                if (years.Count > 0)
                {
                    int minYear = years.Min();
                    int maxYear = years.Max();
                    if (maxYear == minYear) return 12;
                    return (maxYear - minYear) * 12;
                }
            }

            if (intervals.Count == 0) return 0;

            // Merge overlapping intervals
            intervals = intervals.OrderBy(i => i.Start).ToList();
            var merged = new List<(DateTime Start, DateTime End)>();
            var current = intervals[0];

            for (int i = 1; i < intervals.Count; i++)
            {
                if (intervals[i].Start <= current.End)
                {
                    if (intervals[i].End > current.End)
                        current = (current.Start, intervals[i].End);
                }
                else
                {
                    merged.Add(current);
                    current = intervals[i];
                }
            }
            merged.Add(current);

            // Calculate total months
            int totalMonths = 0;
            foreach (var interval in merged)
            {
                totalMonths += ((interval.End.Year - interval.Start.Year) * 12) + interval.End.Month - interval.Start.Month;
            }

            return Math.Max(0, totalMonths);
        }

        private DateTime ParseDate(string dateStr, bool isStart)
        {
            var yearMatch = Regex.Match(dateStr, @"(?:19|20)\d{2}");
            if (!yearMatch.Success) return isStart ? DateTime.Now : DateTime.Now;

            int year = int.Parse(yearMatch.Value);
            int month = isStart ? 1 : 12; // Default to Jan for start, Dec for end if month missing

            var monthNames = new[] { "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec" };
            var lowerDate = dateStr.ToLower();
            for (int i = 0; i < monthNames.Length; i++)
            {
                if (lowerDate.Contains(monthNames[i]))
                {
                    month = i + 1;
                    break;
                }
            }

            var numMonthMatch = Regex.Match(dateStr, @"(\d{1,2})[/-]");
            if (numMonthMatch.Success && int.TryParse(numMonthMatch.Groups[1].Value, out int m) && m >= 1 && m <= 12)
            {
                month = m;
            }

            return new DateTime(year, month, 1);
        }
    }
}

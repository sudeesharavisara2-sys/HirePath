using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using HirePathAI.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace HirePathAI.Infrastructure.AI.Services
{
    public class EnhancedPdfService : IPdfExtractor
    {
        private readonly ILogger<EnhancedPdfService> _logger;
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

        public EnhancedPdfService(ILogger<EnhancedPdfService> logger)
        {
            _logger = logger;
        }

        public string ExtractText(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            try
            {
                using var stream = file.OpenReadStream();
                using var document = PdfDocument.Open(stream);

                var totalPages = document.NumberOfPages;
                var allText = new StringBuilder();
                var pageTexts = new List<string>();

                for (int i = 1; i <= totalPages; i++)
                {
                    var page = document.GetPage(i);
                    var pageText = ExtractPageTextStructured(page);
                    pageTexts.Add(pageText);
                }

                var headers = DetectRepeatedHeaders(pageTexts);

                for (int i = 0; i < pageTexts.Count; i++)
                {
                    var cleaned = RemoveHeaderFooter(pageTexts[i], headers);
                    allText.AppendLine($"--- PAGE {i + 1} ---");
                    allText.AppendLine(cleaned);
                }

                return CleanExtractedText(allText.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enhanced PDF extraction failed for {FileName}", file.FileName);
                throw new InvalidOperationException($"Failed to extract text from PDF: {ex.Message}");
            }
        }

        private string ExtractPageTextStructured(Page page)
        {
            var words = page.GetWords().ToList();
            if (words.Count == 0)
                return string.Empty;

            var pageWidth = page.Width;
            var pageHeight = page.Height;

            var tolerance = pageWidth * 0.02;

            var lines = words
                .GroupBy(w => Math.Round(w.BoundingBox.Bottom / 2) * 2)
                .OrderByDescending(g => g.Key)
                .Select(g => g.OrderBy(w => w.BoundingBox.Left))
                .ToList();

            var result = new StringBuilder();
            foreach (var line in lines)
            {
                var lineText = string.Join(" ", line.Select(w => w.Text));
                result.AppendLine(lineText.Trim());
            }

            return result.ToString();
        }

        private List<string> DetectRepeatedHeaders(List<string> pageTexts)
        {
            if (pageTexts.Count < 2) return new();

            var candidates = new List<string>();
            var firstPageLines = pageTexts[0]
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => l.Length > 3)
                .Take(3)
                .ToList();

            if (firstPageLines.Count == 0) return candidates;

            foreach (var line in firstPageLines)
            {
                int matchCount = 0;
                for (int i = 1; i < pageTexts.Count; i++)
                {
                    if (pageTexts[i].Contains(line))
                        matchCount++;
                }
                if (matchCount >= pageTexts.Count * 0.5)
                    candidates.Add(line);
            }

            var lastPageLines = pageTexts[^1]
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => l.Length > 3)
                .TakeLast(3)
                .ToList();

            foreach (var line in lastPageLines)
            {
                int matchCount = 0;
                for (int i = 0; i < pageTexts.Count - 1; i++)
                {
                    if (pageTexts[i].Contains(line))
                        matchCount++;
                }
                if (matchCount >= pageTexts.Count * 0.5)
                    candidates.Add(line);
            }

            return candidates.Distinct().ToList();
        }

        private string RemoveHeaderFooter(string pageText, List<string> headers)
        {
            var result = pageText;
            foreach (var header in headers)
            {
                result = result.Replace(header, "");
            }
            return result.Trim();
        }

        private string CleanExtractedText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            text = Regex.Replace(text, @"[\x00-\x08\x0B\x0C\x0E-\x1F]", "");
            text = Regex.Replace(text, @"[^\x20-\x7E\s]", m =>
            {
                var c = m.Value[0];
                return c switch
                {
                    '\u2013' or '\u2014' => "-",
                    '\u2018' or '\u2019' or '\u201A' or '\u201B' => "'",
                    '\u201C' or '\u201D' or '\u201E' or '\u201F' => "\"",
                    '\u2022' or '\u2023' or '\u25E6' or '\u2043' => "*",
                    '\u2026' => "...",
                    '\u00A9' => "(c)",
                    '\u00AE' => "(r)",
                    '\u20AC' => "EUR",
                    '\u00A3' => "GBP",
                    '\u00A5' => "JPY",
                    '\u00A0' => " ",
                    _ => c > 127 ? " " : m.Value
                };
            });

            text = Regex.Replace(text, @"[ \t]+", " ");
            text = Regex.Replace(text, @"\n{3,}", "\n\n");
            text = Regex.Replace(text, @"^\s+", "", RegexOptions.Multiline);

            return text.Trim();
        }

        public (bool IsValid, string ErrorMessage) Validate(IFormFile file)
        {
            if (file == null)
                return (false, "No file selected.");

            if (file.Length == 0)
                return (false, "File is empty.");

            if (file.Length > MaxFileSizeBytes)
                return (false, "File size exceeds 10MB limit.");

            if (!Path.GetExtension(file.FileName).ToLower().Equals(".pdf"))
                return (false, "Only PDF files are allowed.");

            if (!file.ContentType.ToLower().Equals("application/pdf"))
                return (false, "Invalid file format. Only PDF files are allowed.");

            return (true, string.Empty);
        }

        public (bool IsResume, string Reason) IsResumeDocument(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return (false, "Empty content");

            if (text.Trim().Length < 50)
                return (false, "Content too short");

            var lowerText = text.ToLower();

            var resumeSectionKeywords = new[] {
                "experience", "skills", "education", "projects", "summary",
                "objective", "employment", "work history", "professional",
                "qualifications", "technical skills", "certifications",
                "achievements", "publications", "languages", "interests",
                "references", "profile", "contact"
            };

            var sectionCount = resumeSectionKeywords.Count(k => lowerText.Contains(k));
            if (sectionCount >= 2)
                return (true, "Valid resume content");

            if (text.Trim().Length >= 200)
                return (true, "Uncertain content - may not be a resume");

            return (false, "Content does not appear to be a resume");
        }
    }
}

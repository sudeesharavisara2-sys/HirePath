using Microsoft.AspNetCore.Http;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Microsoft.Extensions.Logging;

namespace HirePathAI.Infrastructure.AI.Services
{
    public class PdfService
    {
        private readonly ILogger<PdfService> _logger;
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

        public PdfService(ILogger<PdfService> logger)
        {
            _logger = logger;
        }

        public string ExtractTextFromPdf(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            try
            {
                using (var stream = file.OpenReadStream())
                using (var document = PdfDocument.Open(stream))
                {
                    var textBuilder = new StringBuilder();

                    foreach (Page page in document.GetPages())
                    {
                        textBuilder.AppendLine(page.Text);
                    }

                    return textBuilder.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract text from PDF: {FileName}", file.FileName);
                throw new InvalidOperationException($"Failed to extract text from PDF: {ex.Message}");
            }
        }

        public (bool IsValid, string ErrorMessage) ValidateFile(IFormFile file)
        {
            if (file == null)
                return (false, "No file selected.");

            if (file.Length == 0)
                return (false, "File is empty.");

            if (file.Length > MaxFileSizeBytes)
                return (false, "File size exceeds 5MB limit.");

            if (!Path.GetExtension(file.FileName).ToLower().Equals(".pdf"))
                return (false, "Only PDF files are allowed.");

            if (!file.ContentType.ToLower().Equals("application/pdf"))
                return (false, "Invalid file format. Only PDF files are allowed.");

            return (true, string.Empty);
        }

        public (bool IsResume, string Reason) IsProbablyResume(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return (false, "Empty content");

            if (text.Trim().Length < 50)
                return (false, "Content too short");

            var lowerText = text.ToLower();

            var academicKeywords = new[] {
                "question", "questions", "answer", "answers", "exam", "examination",
                "paper", "question paper", "marks", "total marks", "section", "part a",
                "part b", "solve", "attempt", "instructions", "time allowed", "maximum marks",
                "semester", "university", "college", "subject", "code", "roll no", "course",
                "chapter", "chapters", "book", "textbook", "notes", "lecture", "presentation",
                "slide", "slides", "powerpoint", "ppt", "faculty", "department", "syllabus",
                "curriculum", "assignment", "homework", "project work", "thesis", "dissertation",
                "research", "study material", "reference", "bibliography", "appendix", "index",
                "table of contents", "introduction", "conclusion", "methodology", "literature review",
                "abstract", "keywords", "doi", "isbn", "volume", "issue", "journal", "publication",
                "author", "authors", "editor", "editors", "publisher", "publication", "edition",
                "copyright", "all rights reserved", "citations", "references", "footnotes", "endnotes"
            };

            var academicCount = academicKeywords.Count(keyword => lowerText.Contains(keyword));

            var resumeKeywords = new[] {
                "experience", "skills", "education", "project", "projects", "summary",
                "objective", "employment", "work", "career", "professional", "qualification",
                "degree", "certification", "technical", "programming", "developer", "engineer"
            };
            var resumeCount = resumeKeywords.Count(keyword => lowerText.Contains(keyword));

            if (academicCount >= 2 && resumeCount < 2)
                return (false, "This appears to be an academic document/book/presentation");

            var businessKeywords = new[] {
                "monthly report", "weekly report", "daily report", "annual report",
                "invoice", "receipt", "bill", "billing", "payment", "transaction",
                "meeting", "meeting minutes", "agenda", "action items", "attendees",
                "budget", "financial", "revenue", "expense", "profit", "loss",
                "quarterly", "fiscal", "fiscal year", "year-end", "year to date",
                "sales", "marketing", "operations", "management", "administration",
                "kpi", "metrics", "dashboard", "analytics", "performance review",
                "target", "goal", "objective", "milestone", "deadline",
                "project status", "progress report", "status update", "summary report",
                "client", "customer", "vendor", "supplier", "contract", "agreement",
                "purchase order", "delivery", "shipment", "inventory", "stock",
                "department", "division", "branch", "headquarters", "office"
            };

            var businessCount = businessKeywords.Count(keyword => lowerText.Contains(keyword));
            if (businessCount >= 2 && resumeCount < 2)
                return (false, "This appears to be a business document/report/invoice");

            if (resumeCount >= 2)
                return (true, "Valid resume content");

            return (true, "Uncertain content - may not be a resume");
        }
    }
}

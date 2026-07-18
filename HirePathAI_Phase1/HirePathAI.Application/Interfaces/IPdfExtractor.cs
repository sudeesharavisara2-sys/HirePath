using Microsoft.AspNetCore.Http;

namespace HirePathAI.Application.Interfaces
{
    public interface IPdfExtractor
    {
        string ExtractText(IFormFile file);
        (bool IsValid, string ErrorMessage) Validate(IFormFile file);
        (bool IsResume, string Reason) IsResumeDocument(string text);
    }
}

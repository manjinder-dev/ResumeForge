namespace ResumeForge.Api.Services;

public interface IPdfTextExtractor
{
    Task<string> ExtractTextAsync(IFormFile pdfFile, CancellationToken cancellationToken);
}

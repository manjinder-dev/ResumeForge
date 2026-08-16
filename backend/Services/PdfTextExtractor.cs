using System.Text;
using ResumeForge.Api.Exceptions;
using UglyToad.PdfPig;

namespace ResumeForge.Api.Services;

public sealed class PdfTextExtractor(ILogger<PdfTextExtractor> logger) : IPdfTextExtractor
{
    public async Task<string> ExtractTextAsync(IFormFile pdfFile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pdfFile);

        try
        {
            await using var sourceStream = pdfFile.OpenReadStream();
            await using var memoryStream = new MemoryStream();
            await sourceStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            using var document = PdfDocument.Open(memoryStream);
            var builder = new StringBuilder();

            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(page.Text))
                {
                    builder.AppendLine(page.Text.Trim());
                }
            }

            var extractedText = builder.ToString().Trim();
            if (string.IsNullOrWhiteSpace(extractedText))
            {
                throw new PdfExtractionException(
                    "No extractable text was found in the PDF. The file may be scanned or image-only. Please upload a text-based PDF.");
            }

            return extractedText;
        }
        catch (PdfExtractionException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PDF text extraction failed.");
            throw new PdfExtractionException(
                "The PDF could not be read. Make sure it is a valid, non-encrypted PDF file.",
                ex);
        }
    }
}

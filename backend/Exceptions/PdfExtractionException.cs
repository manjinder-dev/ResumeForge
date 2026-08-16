namespace ResumeForge.Api.Exceptions;

public sealed class PdfExtractionException : Exception
{
    public PdfExtractionException(string message)
        : base(message)
    {
    }

    public PdfExtractionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

using System.Net;

namespace ResumeForge.Api.Exceptions;

public sealed class OpenAiApiException : Exception
{
    public OpenAiApiException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}

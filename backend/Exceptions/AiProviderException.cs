using System.Net;

namespace ResumeForge.Api.Exceptions;

public sealed class AiProviderException : Exception
{
    public AiProviderException(string provider, HttpStatusCode statusCode, string message)
        : base(message)
    {
        Provider = provider;
        StatusCode = statusCode;
    }

    public string Provider { get; }

    public HttpStatusCode StatusCode { get; }
}

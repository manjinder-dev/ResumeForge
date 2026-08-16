using System.Net;

namespace ResumeForge.Api.Services;

public interface IEndpointSecurityValidator
{
    Task<IReadOnlyList<IPAddress>> ResolvePublicHttpsEndpointAsync(
        Uri endpoint,
        CancellationToken cancellationToken);
}

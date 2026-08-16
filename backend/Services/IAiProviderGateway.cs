using ResumeForge.Api.Models;

namespace ResumeForge.Api.Services;

public interface IAiProviderGateway
{
    Task<string> GenerateJsonAsync(
        AiProviderOptions options,
        string prompt,
        CancellationToken cancellationToken);
}

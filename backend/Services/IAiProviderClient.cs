using ResumeForge.Api.Models;

namespace ResumeForge.Api.Services;

public interface IAiProviderClient
{
    bool CanHandle(string provider);

    Task<string> GenerateJsonAsync(
        AiProviderOptions options,
        string prompt,
        CancellationToken cancellationToken);
}

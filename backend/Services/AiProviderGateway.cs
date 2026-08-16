using ResumeForge.Api.Models;

namespace ResumeForge.Api.Services;

public sealed class AiProviderGateway(IEnumerable<IAiProviderClient> providerClients) : IAiProviderGateway
{
    public Task<string> GenerateJsonAsync(
        AiProviderOptions options,
        string prompt,
        CancellationToken cancellationToken)
    {
        var client = providerClients.FirstOrDefault(candidate => candidate.CanHandle(options.Provider));
        if (client is null)
        {
            throw new ArgumentException($"Unsupported AI provider '{options.Provider}'.", nameof(options));
        }

        return client.GenerateJsonAsync(options, prompt, cancellationToken);
    }
}

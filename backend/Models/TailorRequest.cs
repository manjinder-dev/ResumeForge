namespace ResumeForge.Api.Models;

public sealed class TailorRequest
{
    public IFormFile? Resume { get; init; }

    public string JobDescription { get; init; } = string.Empty;

    public string Provider { get; init; } = AiProviderCatalog.OpenAi;

    public string Model { get; init; } = string.Empty;

    public string? CustomBaseUrl { get; init; }
}

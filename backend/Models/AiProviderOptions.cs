namespace ResumeForge.Api.Models;

public sealed record AiProviderOptions(
    string Provider,
    string ApiKey,
    string Model,
    string? CustomBaseUrl);

using System.Text.Json.Serialization;

namespace ResumeForge.Api.Models;

public sealed record TailoredResumeResult(
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("bulletPoints")] IReadOnlyList<string> BulletPoints,
    [property: JsonPropertyName("coverLetter")] string CoverLetter);

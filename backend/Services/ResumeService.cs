using System.Net;
using System.Text.Json;
using ResumeForge.Api.Exceptions;
using ResumeForge.Api.Models;

namespace ResumeForge.Api.Services;

public sealed class ResumeService(
    IAiProviderGateway aiProviderGateway,
    ILogger<ResumeService> logger) : IResumeService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<TailoredResumeResult> TailorAsync(
        string resumeText,
        string jobDescription,
        AiProviderOptions aiOptions,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(resumeText))
        {
            throw new ArgumentException("Resume text is required.", nameof(resumeText));
        }

        if (string.IsNullOrWhiteSpace(jobDescription))
        {
            throw new ArgumentException("Job description is required.", nameof(jobDescription));
        }

        if (string.IsNullOrWhiteSpace(aiOptions.ApiKey))
        {
            throw new ArgumentException("AI provider API key is required.", nameof(aiOptions));
        }

        var prompt = BuildPrompt(resumeText, jobDescription);
        var content = await aiProviderGateway.GenerateJsonAsync(aiOptions, prompt, cancellationToken);
        var json = ExtractJsonObject(content, aiOptions.Provider);

        TailoredResumeResult? result;
        try
        {
            result = JsonSerializer.Deserialize<TailoredResumeResult>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "AI provider {Provider} returned malformed resume JSON.", aiOptions.Provider);
            throw new AiProviderException(
                aiOptions.Provider,
                HttpStatusCode.BadGateway,
                $"{aiOptions.Provider} returned malformed JSON. Please try again.");
        }

        if (result is null ||
            string.IsNullOrWhiteSpace(result.Summary) ||
            result.BulletPoints is null ||
            result.BulletPoints.Count == 0 ||
            result.BulletPoints.Any(point => string.IsNullOrWhiteSpace(point)) ||
            string.IsNullOrWhiteSpace(result.CoverLetter))
        {
            throw new AiProviderException(
                aiOptions.Provider,
                HttpStatusCode.BadGateway,
                $"{aiOptions.Provider} returned incomplete resume content. Please try again.");
        }

        return result;
    }

    private static string BuildPrompt(string resumeText, string jobDescription) =>
        $$"""
        You are a Senior Resume Writer for FAANG-level companies.

        Original Resume Text:
        {{resumeText}}

        Target Job Description:
        {{jobDescription}}

        Instructions:

        1. Rewrite the 'Experience' bullet points to include specific keywords from the Job Description.
        2. Quantify achievements (add numbers, percentages, and dollar amounts where missing).
        3. Write a 3-sentence 'Professional Summary' tailored exactly to this role.
        4. Write a 4-sentence Cover Letter that connects their past projects to the new company's mission.

        Output ONLY valid JSON in this format:
        {
        "summary": "...",
        "bulletPoints": ["point1", "point2", "point3"],
        "coverLetter": "..."
        }
        """;

    private static string ExtractJsonObject(string content, string provider)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewLine = trimmed.IndexOf('\n');
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewLine >= 0 && lastFence > firstNewLine)
            {
                trimmed = trimmed[(firstNewLine + 1)..lastFence].Trim();
            }
        }

        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');
        if (firstBrace < 0 || lastBrace <= firstBrace)
        {
            throw new AiProviderException(
                provider,
                HttpStatusCode.BadGateway,
                $"{provider} did not return a JSON object.");
        }

        return trimmed[firstBrace..(lastBrace + 1)];
    }
}

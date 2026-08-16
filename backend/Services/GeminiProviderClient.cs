using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ResumeForge.Api.Exceptions;
using ResumeForge.Api.Models;

namespace ResumeForge.Api.Services;

public sealed class GeminiProviderClient(
    IHttpClientFactory httpClientFactory,
    ILogger<GeminiProviderClient> logger) : IAiProviderClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public bool CanHandle(string provider) =>
        string.Equals(provider, AiProviderCatalog.Gemini, StringComparison.OrdinalIgnoreCase);

    public async Task<string> GenerateJsonAsync(
        AiProviderOptions options,
        string prompt,
        CancellationToken cancellationToken)
    {
        var model = options.Model.Trim();
        if (model.StartsWith("models/", StringComparison.OrdinalIgnoreCase))
        {
            model = model["models/".Length..];
        }

        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent";
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                maxOutputTokens = 4096
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(requestBody, options: JsonOptions)
        };

        request.Headers.TryAddWithoutValidation("x-goog-api-key", options.ApiKey.Trim());

        var client = httpClientFactory.CreateClient("AiProviders");
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Gemini request failed with status code {StatusCode}.", (int)response.StatusCode);
            throw new AiProviderException(
                AiProviderCatalog.Gemini,
                response.StatusCode,
                ExtractErrorMessage(responseBody) ?? "Gemini rejected the request.");
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var candidates = document.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("Gemini returned no candidates.");
            }

            var parts = candidates[0].GetProperty("content").GetProperty("parts");
            var builder = new StringBuilder();
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                {
                    builder.Append(text.GetString());
                }
            }

            var result = builder.ToString();
            if (string.IsNullOrWhiteSpace(result))
            {
                throw new InvalidOperationException("Gemini returned no text content.");
            }

            return result;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(ex, "Gemini returned an unreadable response.");
            throw new AiProviderException(
                AiProviderCatalog.Gemini,
                HttpStatusCode.BadGateway,
                "Gemini returned an unreadable response.");
        }
    }

    private static string? ExtractErrorMessage(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message))
            {
                return message.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }
}

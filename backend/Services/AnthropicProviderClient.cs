using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ResumeForge.Api.Exceptions;
using ResumeForge.Api.Models;

namespace ResumeForge.Api.Services;

public sealed class AnthropicProviderClient(
    IHttpClientFactory httpClientFactory,
    ILogger<AnthropicProviderClient> logger) : IAiProviderClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public bool CanHandle(string provider) =>
        string.Equals(provider, AiProviderCatalog.Anthropic, StringComparison.OrdinalIgnoreCase);

    public async Task<string> GenerateJsonAsync(
        AiProviderOptions options,
        string prompt,
        CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            model = options.Model,
            max_tokens = 8192,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = JsonContent.Create(requestBody, options: JsonOptions)
        };

        request.Headers.TryAddWithoutValidation("x-api-key", options.ApiKey.Trim());
        request.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");

        var client = httpClientFactory.CreateClient("AiProviders");
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Anthropic request failed with status code {StatusCode}.", (int)response.StatusCode);
            throw new AiProviderException(
                AiProviderCatalog.Anthropic,
                response.StatusCode,
                ExtractErrorMessage(responseBody) ?? "Anthropic rejected the request.");
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var content = document.RootElement.GetProperty("content");
            var builder = new StringBuilder();

            foreach (var item in content.EnumerateArray())
            {
                if (item.TryGetProperty("type", out var type) &&
                    string.Equals(type.GetString(), "text", StringComparison.OrdinalIgnoreCase) &&
                    item.TryGetProperty("text", out var text))
                {
                    builder.Append(text.GetString());
                }
            }

            var result = builder.ToString();
            if (string.IsNullOrWhiteSpace(result))
            {
                throw new InvalidOperationException("Anthropic returned no text content.");
            }

            return result;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(ex, "Anthropic returned an unreadable response.");
            throw new AiProviderException(
                AiProviderCatalog.Anthropic,
                HttpStatusCode.BadGateway,
                "Anthropic returned an unreadable response.");
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

using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ResumeForge.Api.Exceptions;
using ResumeForge.Api.Models;

namespace ResumeForge.Api.Services;

public sealed class OpenAiCompatibleProviderClient(
    IHttpClientFactory httpClientFactory,
    IEndpointSecurityValidator endpointSecurityValidator,
    ILogger<OpenAiCompatibleProviderClient> logger) : IAiProviderClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyDictionary<string, ProviderConfiguration> ProviderConfigurations =
        new Dictionary<string, ProviderConfiguration>(StringComparer.OrdinalIgnoreCase)
        {
            [AiProviderCatalog.OpenAi] = new("https://api.openai.com/v1/chat/completions", true),
            [AiProviderCatalog.XAi] = new("https://api.x.ai/v1/chat/completions", false),
            [AiProviderCatalog.Groq] = new("https://api.groq.com/openai/v1/chat/completions", false),
            [AiProviderCatalog.DeepSeek] = new("https://api.deepseek.com/chat/completions", false),
            [AiProviderCatalog.Mistral] = new("https://api.mistral.ai/v1/chat/completions", false),
            [AiProviderCatalog.OpenRouter] = new("https://openrouter.ai/api/v1/chat/completions", false)
        };

    public bool CanHandle(string provider) =>
        ProviderConfigurations.ContainsKey(provider) ||
        string.Equals(provider, AiProviderCatalog.Custom, StringComparison.OrdinalIgnoreCase);

    public async Task<string> GenerateJsonAsync(
        AiProviderOptions options,
        string prompt,
        CancellationToken cancellationToken)
    {
        var (endpoint, useJsonMode, pinnedAddresses) = await ResolveEndpointAsync(options, cancellationToken);

        var requestBody = new Dictionary<string, object?>
        {
            ["model"] = options.Model,
            ["messages"] = new[]
            {
                new Dictionary<string, string>
                {
                    ["role"] = "user",
                    ["content"] = prompt
                }
            }
        };

        if (useJsonMode)
        {
            requestBody["response_format"] = new Dictionary<string, string>
            {
                ["type"] = "json_object"
            };
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(requestBody, options: JsonOptions)
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey.Trim());

        if (string.Equals(options.Provider, AiProviderCatalog.OpenRouter, StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.TryAddWithoutValidation("X-OpenRouter-Title", "ResumeForge");
        }

        using var customClient = pinnedAddresses is null
            ? null
            : CreatePinnedHttpsClient(pinnedAddresses);

        var client = customClient ?? httpClientFactory.CreateClient("AiProviders");
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "AI provider {Provider} request failed with status code {StatusCode}.",
                options.Provider,
                (int)response.StatusCode);

            throw new AiProviderException(
                options.Provider,
                response.StatusCode,
                ExtractErrorMessage(responseBody) ?? $"{options.Provider} rejected the request.");
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var choices = document.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("The provider returned no completion choices.");
            }

            var content = choices[0].GetProperty("message").GetProperty("content");
            var text = ExtractContentText(content);

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("The provider returned an empty completion.");
            }

            return text;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(ex, "AI provider {Provider} returned an unreadable response.", options.Provider);
            throw new AiProviderException(
                options.Provider,
                HttpStatusCode.BadGateway,
                $"{options.Provider} returned an unreadable response.");
        }
    }

    private async Task<(Uri Endpoint, bool UseJsonMode, IReadOnlyList<IPAddress>? PinnedAddresses)> ResolveEndpointAsync(
        AiProviderOptions options,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(options.Provider, AiProviderCatalog.Custom, StringComparison.OrdinalIgnoreCase))
        {
            var configuration = ProviderConfigurations[options.Provider];
            return (new Uri(configuration.Endpoint), configuration.UseJsonMode, null);
        }

        if (string.IsNullOrWhiteSpace(options.CustomBaseUrl))
        {
            throw new ArgumentException("A custom base URL is required for the Other provider.");
        }

        var endpointText = options.CustomBaseUrl.Trim().TrimEnd('/');
        if (!endpointText.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            endpointText += "/chat/completions";
        }

        if (!Uri.TryCreate(endpointText, UriKind.Absolute, out var endpoint))
        {
            throw new ArgumentException("The custom AI base URL is invalid.");
        }

        var publicAddresses = await endpointSecurityValidator.ResolvePublicHttpsEndpointAsync(endpoint, cancellationToken);
        return (endpoint, false, publicAddresses);
    }


    private static HttpClient CreatePinnedHttpsClient(IReadOnlyList<IPAddress> addresses)
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            ConnectCallback = async (context, cancellationToken) =>
            {
                Exception? lastError = null;

                foreach (var address in addresses)
                {
                    var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                    {
                        NoDelay = true
                    };

                    try
                    {
                        await socket.ConnectAsync(
                            new IPEndPoint(address, context.DnsEndPoint.Port),
                            cancellationToken);

                        return new NetworkStream(socket, ownsSocket: true);
                    }
                    catch (Exception ex) when (ex is SocketException or OperationCanceledException)
                    {
                        socket.Dispose();
                        lastError = ex;

                        if (ex is OperationCanceledException)
                        {
                            throw;
                        }
                    }
                }

                throw new HttpRequestException(
                    "Could not connect to the validated custom AI endpoint.",
                    lastError);
            }
        };

        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(90)
        };
    }

    private static string ExtractContentText(JsonElement content)
    {
        if (content.ValueKind == JsonValueKind.String)
        {
            return content.GetString() ?? string.Empty;
        }

        if (content.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var item in content.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object &&
                item.TryGetProperty("text", out var textElement) &&
                textElement.ValueKind == JsonValueKind.String)
            {
                builder.Append(textElement.GetString());
            }
        }

        return builder.ToString();
    }

    private static string? ExtractErrorMessage(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (!document.RootElement.TryGetProperty("error", out var error))
            {
                return null;
            }

            if (error.ValueKind == JsonValueKind.String)
            {
                return error.GetString();
            }

            if (error.ValueKind == JsonValueKind.Object &&
                error.TryGetProperty("message", out var message) &&
                message.ValueKind == JsonValueKind.String)
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

    private sealed record ProviderConfiguration(string Endpoint, bool UseJsonMode);
}

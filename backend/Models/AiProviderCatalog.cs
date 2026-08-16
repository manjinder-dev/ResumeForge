namespace ResumeForge.Api.Models;

public static class AiProviderCatalog
{
    public const string OpenAi = "openai";
    public const string Anthropic = "anthropic";
    public const string Gemini = "gemini";
    public const string XAi = "xai";
    public const string Groq = "groq";
    public const string DeepSeek = "deepseek";
    public const string Mistral = "mistral";
    public const string OpenRouter = "openrouter";
    public const string Custom = "custom";

    private static readonly IReadOnlyDictionary<string, string> DefaultModels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [OpenAi] = "gpt-4o-mini",
            [Anthropic] = "claude-sonnet-5",
            [Gemini] = "gemini-3.7-flash",
            [XAi] = "grok-4.6",
            [Groq] = "llama-3.3-70b-versatile",
            [DeepSeek] = "deepseek-v4-pro",
            [Mistral] = "mistral-small-latest",
            [OpenRouter] = "openai/gpt-4o-mini",
            [Custom] = ""
        };

    public static bool IsSupported(string provider) =>
        !string.IsNullOrWhiteSpace(provider) && DefaultModels.ContainsKey(provider.Trim());

    public static string GetDefaultModel(string provider)
    {
        if (!IsSupported(provider))
        {
            throw new ArgumentException("Unsupported AI provider.", nameof(provider));
        }

        return DefaultModels[provider.Trim()];
    }
}

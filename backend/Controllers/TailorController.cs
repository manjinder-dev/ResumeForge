using System.Net;
using Microsoft.AspNetCore.Mvc;
using ResumeForge.Api.Exceptions;
using ResumeForge.Api.Models;
using ResumeForge.Api.Services;

namespace ResumeForge.Api.Controllers;

[ApiController]
[Route("api/tailor")]
public sealed class TailorController(
    IPdfTextExtractor pdfTextExtractor,
    IResumeService resumeService,
    ILogger<TailorController> logger) : ControllerBase
{
    private const long MaxPdfSizeBytes = 10 * 1024 * 1024;

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<TailoredResumeResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiError>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ApiError>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ApiError>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ApiError>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<TailoredResumeResult>> Tailor(
        [FromForm] TailorRequest request,
        [FromHeader(Name = "X-AI-Key")] string? aiApiKey,
        [FromHeader(Name = "X-OpenAI-Key")] string? legacyOpenAiApiKey,
        CancellationToken cancellationToken)
    {
        if (request.Resume is null || request.Resume.Length == 0)
        {
            return BadRequest(new ApiError("invalid_resume", "A PDF resume is required."));
        }

        if (request.Resume.Length > MaxPdfSizeBytes)
        {
            return StatusCode(
                StatusCodes.Status413PayloadTooLarge,
                new ApiError("resume_too_large", "The PDF must be 10 MB or smaller."));
        }

        var extension = Path.GetExtension(request.Resume.FileName);
        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Resume.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ApiError("invalid_file_type", "Only PDF resumes are supported."));
        }

        if (string.IsNullOrWhiteSpace(request.JobDescription) || request.JobDescription.Trim().Length < 50)
        {
            return BadRequest(new ApiError(
                "invalid_job_description",
                "Job description must contain at least 50 characters."));
        }

        if (request.JobDescription.Length > 30_000)
        {
            return BadRequest(new ApiError(
                "job_description_too_large",
                "Job description must contain 30,000 characters or fewer."));
        }

        var provider = string.IsNullOrWhiteSpace(request.Provider)
            ? AiProviderCatalog.OpenAi
            : request.Provider.Trim().ToLowerInvariant();

        if (!AiProviderCatalog.IsSupported(provider))
        {
            return BadRequest(new ApiError(
                "unsupported_provider",
                "The selected AI provider is not supported."));
        }

        var apiKey = !string.IsNullOrWhiteSpace(aiApiKey)
            ? aiApiKey.Trim()
            : legacyOpenAiApiKey?.Trim();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return BadRequest(new ApiError("missing_api_key", "An AI provider API key is required."));
        }

        var model = string.IsNullOrWhiteSpace(request.Model)
            ? AiProviderCatalog.GetDefaultModel(provider)
            : request.Model.Trim();

        if (string.IsNullOrWhiteSpace(model) || model.Length > 200)
        {
            return BadRequest(new ApiError("invalid_model", "A valid AI model name is required."));
        }

        if (string.Equals(provider, AiProviderCatalog.Custom, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.CustomBaseUrl))
            {
                return BadRequest(new ApiError(
                    "missing_custom_base_url",
                    "A base URL is required for the Other provider."));
            }

            if (request.CustomBaseUrl.Length > 2_048)
            {
                return BadRequest(new ApiError(
                    "invalid_custom_base_url",
                    "The custom base URL is too long."));
            }
        }

        try
        {
            var resumeText = await pdfTextExtractor.ExtractTextAsync(request.Resume, cancellationToken);
            var aiOptions = new AiProviderOptions(
                provider,
                apiKey,
                model,
                request.CustomBaseUrl?.Trim());

            var result = await resumeService.TailorAsync(
                resumeText,
                request.JobDescription.Trim(),
                aiOptions,
                cancellationToken);

            return Ok(result);
        }
        catch (PdfExtractionException ex)
        {
            return UnprocessableEntity(new ApiError("pdf_extraction_failed", ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiError("invalid_ai_configuration", ex.Message));
        }
        catch (AiProviderException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized ||
                                              ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return Unauthorized(new ApiError(
                "ai_provider_unauthorized",
                $"{DisplayProvider(ex.Provider)} rejected the API key or denied access."));
        }
        catch (AiProviderException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return StatusCode(
                StatusCodes.Status429TooManyRequests,
                new ApiError("ai_provider_rate_limited", ex.Message));
        }
        catch (AiProviderException ex)
        {
            var providerStatusCode = (int)ex.StatusCode;
            var statusCode = providerStatusCode >= StatusCodes.Status400BadRequest &&
                             providerStatusCode < StatusCodes.Status500InternalServerError
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status502BadGateway;

            return StatusCode(
                statusCode,
                new ApiError("ai_provider_error", ex.Message));
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Resume tailoring request was cancelled by the client.");
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while tailoring a resume.");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ApiError("internal_error", "An unexpected error occurred."));
        }
    }

    private static string DisplayProvider(string provider) => provider switch
    {
        AiProviderCatalog.OpenAi => "OpenAI",
        AiProviderCatalog.Anthropic => "Anthropic",
        AiProviderCatalog.Gemini => "Google Gemini",
        AiProviderCatalog.XAi => "xAI",
        AiProviderCatalog.Groq => "Groq",
        AiProviderCatalog.DeepSeek => "DeepSeek",
        AiProviderCatalog.Mistral => "Mistral",
        AiProviderCatalog.OpenRouter => "OpenRouter",
        AiProviderCatalog.Custom => "Custom AI provider",
        _ => "AI provider"
    };
}

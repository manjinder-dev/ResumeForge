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

        AiTailoringPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<AiTailoringPayload>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "AI provider {Provider} returned malformed resume JSON.", aiOptions.Provider);
            throw new AiProviderException(
                aiOptions.Provider,
                HttpStatusCode.BadGateway,
                $"{aiOptions.Provider} returned malformed JSON. Please try again.");
        }

        if (payload is null || payload.Resume is null || payload.CoverLetter is null)
        {
            throw new AiProviderException(
                aiOptions.Provider,
                HttpStatusCode.BadGateway,
                $"{aiOptions.Provider} returned incomplete resume content. Please try again.");
        }

        var resume = NormalizeResume(payload.Resume);
        var coverLetterDocument = NormalizeCoverLetter(payload.CoverLetter);
        ValidatePayload(resume, coverLetterDocument, aiOptions.Provider);
        var allBulletPoints = resume.Experience
            .SelectMany(experience => experience.BulletPoints)
            .Where(point => !string.IsNullOrWhiteSpace(point))
            .ToArray();

        return new TailoredResumeResult(
            resume.Summary,
            allBulletPoints,
            coverLetterDocument.Content,
            resume,
            coverLetterDocument);
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

        Additional document-generation rules:
        - Reconstruct the complete candidate resume into the JSON structure below so ResumeForge can generate a professional DOCX and PDF.
        - Preserve the candidate's real name, email, phone, location, employers, job titles, employment dates, education, certifications, projects, links, and other factual details from the original resume. Never change factual dates, employers, degrees, or contact information.
        - Do not invent companies, job titles, degrees, certifications, technologies, projects, URLs, phone numbers, email addresses, locations, or dates.
        - Use an empty string or empty array when a field is not present in the original resume or target job description.
        - Keep experience in reverse chronological order.
        - Tailor skills and wording to the target role while retaining only skills reasonably supported by the original resume.
        - Keep each experience bullet concise, achievement-focused, and ATS-friendly.
        - Infer companyName and jobTitle for the cover letter only when they are clearly identifiable from the target Job Description; otherwise use an empty string.
        - Set recipient to "Hiring Manager" unless a recipient name is explicitly present in the Job Description.
        - The coverLetter.content must be exactly 4 sentences and must not include a postal-address block, greeting, sign-off, or signature because ResumeForge adds those during export.

        Output ONLY valid JSON in this exact structure:
        {
          "resume": {
            "fullName": "",
            "headline": "",
            "contact": {
              "email": "",
              "phone": "",
              "location": "",
              "linkedIn": "",
              "portfolio": ""
            },
            "summary": "",
            "skills": [""],
            "experience": [
              {
                "jobTitle": "",
                "company": "",
                "location": "",
                "startDate": "",
                "endDate": "",
                "bulletPoints": [""]
              }
            ],
            "projects": [
              {
                "name": "",
                "description": "",
                "technologies": [""]
              }
            ],
            "education": [
              {
                "degree": "",
                "institution": "",
                "location": "",
                "graduationDate": "",
                "details": [""]
              }
            ],
            "certifications": ["" ]
          },
          "coverLetter": {
            "recipient": "Hiring Manager",
            "companyName": "",
            "jobTitle": "",
            "content": ""
          }
        }
        """;

    private static void ValidatePayload(
        ProfessionalResume resume,
        CoverLetterDocument coverLetter,
        string provider)
    {
        if (string.IsNullOrWhiteSpace(resume.Summary) ||
            resume.Experience.Count == 0 ||
            resume.Experience.Any(experience =>
                string.IsNullOrWhiteSpace(experience.JobTitle) ||
                string.IsNullOrWhiteSpace(experience.Company) ||
                experience.BulletPoints.Count == 0) ||
            string.IsNullOrWhiteSpace(coverLetter.Content))
        {
            throw new AiProviderException(
                provider,
                HttpStatusCode.BadGateway,
                $"{provider} returned incomplete resume content. Please try again.");
        }
    }

    private static ProfessionalResume NormalizeResume(ProfessionalResume source)
    {
        var contactSource = source.Contact ?? new ResumeContact(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        var contact = new ResumeContact(
            Clean(contactSource.Email),
            Clean(contactSource.Phone),
            Clean(contactSource.Location),
            Clean(contactSource.LinkedIn),
            Clean(contactSource.Portfolio));

        var experience = (source.Experience ?? [])
            .Where(item => item is not null)
            .Select(item => new ResumeExperience(
                Clean(item.JobTitle),
                Clean(item.Company),
                Clean(item.Location),
                Clean(item.StartDate),
                Clean(item.EndDate),
                CleanList(item.BulletPoints)))
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.JobTitle) ||
                !string.IsNullOrWhiteSpace(item.Company) ||
                item.BulletPoints.Count > 0)
            .ToArray();

        var projects = (source.Projects ?? [])
            .Where(item => item is not null)
            .Select(item => new ResumeProject(
                Clean(item.Name),
                Clean(item.Description),
                CleanList(item.Technologies)))
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.Name) ||
                !string.IsNullOrWhiteSpace(item.Description) ||
                item.Technologies.Count > 0)
            .ToArray();

        var education = (source.Education ?? [])
            .Where(item => item is not null)
            .Select(item => new ResumeEducation(
                Clean(item.Degree),
                Clean(item.Institution),
                Clean(item.Location),
                Clean(item.GraduationDate),
                CleanList(item.Details)))
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.Degree) ||
                !string.IsNullOrWhiteSpace(item.Institution) ||
                item.Details.Count > 0)
            .ToArray();

        return new ProfessionalResume(
            Clean(source.FullName),
            Clean(source.Headline),
            contact,
            Clean(source.Summary),
            CleanList(source.Skills),
            experience,
            projects,
            education,
            CleanList(source.Certifications));
    }

    private static CoverLetterDocument NormalizeCoverLetter(CoverLetterDocument source) =>
        new(
            string.IsNullOrWhiteSpace(source.Recipient) ? "Hiring Manager" : source.Recipient.Trim(),
            Clean(source.CompanyName),
            Clean(source.JobTitle),
            Clean(source.Content));

    private static IReadOnlyList<string> CleanList(IReadOnlyList<string>? values) =>
        (values ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToArray();

    private static string Clean(string? value) => value?.Trim() ?? string.Empty;

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

    private sealed record AiTailoringPayload(
        ProfessionalResume Resume,
        CoverLetterDocument CoverLetter);
}

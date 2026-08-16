# ResumeForge

ResumeForge is a stateless full-stack application that tailors a text-based PDF resume to a target job description. The Angular frontend uploads the PDF and job description to a .NET 8 Web API, the backend extracts text with PdfPig, and the extracted resume content is sent to the AI provider selected by the user with the user's own API key.

## Features

- Angular 18 standalone-component frontend with Signals and OnPush change detection.
- PDF resume upload with client-side and server-side file validation.
- Pure C# PDF text extraction using PdfPig.
- Clear rejection of scanned or image-only PDFs when no extractable text is present.
- Multi-provider AI support with provider-specific API adapters.
- Per-provider API keys stored only in browser `localStorage` and transmitted to the backend in `X-AI-Key`.
- Editable model ID so model changes do not require a ResumeForge redeployment.
- Structured JSON result containing a professional summary, rewritten bullet points, and cover letter.
- Browser-side `.txt` and `.docx` downloads.
- No database and no server-side persistence of resumes, job descriptions, API keys, or generated results.
- Multi-stage Docker builds, Docker Compose networking, and container health checks.
- A guarded `Other (OpenAI-compatible)` option for public HTTPS endpoints.

## Supported AI Providers

| Provider | Provider ID | Default model | API style |
| --- | --- | --- | --- |
| OpenAI | `openai` | `gpt-4o-mini` | OpenAI Chat Completions |
| Anthropic / Claude | `anthropic` | `claude-sonnet-5` | Anthropic Messages API |
| Google Gemini | `gemini` | `gemini-3.7-flash` | Gemini `generateContent` |
| xAI / Grok | `xai` | `grok-4.6` | OpenAI-compatible Chat Completions |
| Groq | `groq` | `llama-3.3-70b-versatile` | OpenAI-compatible Chat Completions |
| DeepSeek | `deepseek` | `deepseek-v4-pro` | OpenAI-compatible Chat Completions |
| Mistral AI | `mistral` | `mistral-small-latest` | OpenAI-compatible Chat Completions |
| OpenRouter | `openrouter` | `openai/gpt-4o-mini` | OpenAI-compatible Chat Completions |
| Other | `custom` | User supplied | Public HTTPS OpenAI-compatible endpoint |

The model field is editable. If a provider deprecates or replaces a model, enter another model ID supported by your account.

The custom provider expects an OpenAI-compatible `/chat/completions` response shape and Bearer-token authentication. If the entered base URL does not already end in `/chat/completions`, ResumeForge appends it. Custom endpoints must use public HTTPS addresses; localhost and private-network destinations are rejected, and the backend does not follow HTTP redirects.

## Architecture

```text
Browser (Angular :4200)
        |
        | POST /api/tailor
        | multipart/form-data
        | X-AI-Key header
        v
Nginx frontend container
        |
        v
.NET 8 API (:8080 internal / :5000 host)
        |
        +--> PdfPig extracts resume text in memory
        |
        +--> Provider adapter
             |-- OpenAI
             |-- Anthropic
             |-- Gemini
             |-- xAI
             |-- Groq
             |-- DeepSeek
             |-- Mistral
             |-- OpenRouter
             `-- Custom OpenAI-compatible endpoint
        |
        v
JSON response -> Angular -> TXT/DOCX download
```

## Quick Start

### Prerequisites

- Docker Engine with Docker Compose support.
- An API key from at least one supported AI provider.

### Run with Docker Compose

Clone this repository with Git, change into the `ResumeForge` directory, then run:

```bash
docker-compose up -d
```

Modern Docker installations can use the equivalent command:

```bash
docker compose up -d
```

Open:

```text
http://localhost:4200
```

Backend health endpoint:

```text
http://localhost:5000/health
```

Stop the application:

```bash
docker-compose down
```

### Local Development Without Docker

Backend:

```bash
cd backend
dotnet restore
dotnet run
```

The included launch profile runs the API at `http://localhost:5000`.

Frontend, in a second terminal:

```bash
cd frontend
npm install
npm start
```

The Angular development server runs at `http://localhost:4200` and uses `proxy.conf.json` to forward `/api` to the .NET API.

## API

### `POST /api/tailor`

Request content type:

```text
multipart/form-data
```

Form fields:

- `resume`: PDF file, maximum 10 MB.
- `jobDescription`: target job description, 50 to 30,000 characters.
- `provider`: one of the provider IDs listed above.
- `model`: model ID. If omitted for a built-in provider, the backend uses that provider's default model.
- `customBaseUrl`: required only when `provider=custom`.

Required request header:

```text
X-AI-Key: <provider-api-key>
```

For backward compatibility, `X-OpenAI-Key` is still accepted when `X-AI-Key` is absent. New clients should use `X-AI-Key`.

Successful response:

```json
{
  "summary": "Tailored professional summary",
  "bulletPoints": [
    "Tailored experience bullet",
    "Another tailored experience bullet"
  ],
  "coverLetter": "Tailored four-sentence cover letter"
}
```

## Provider Key Storage

ResumeForge stores API keys separately for each provider in the browser under `resumeforge_ai_keys_v1`. Switching providers restores that provider's previously entered key. Provider selection, model overrides, and the custom base URL are stored under `resumeforge_ai_settings_v1`.

An older OpenAI key stored under `resumeforge_openai_key` is migrated automatically to the new per-provider key store the first time the updated frontend loads.

Browser `localStorage` is convenient but is accessible to JavaScript running on the same origin. For a public production deployment, a strong Content Security Policy and careful XSS prevention are mandatory. If you do not want the browser to retain a key, use **Clear saved key** after the request.

## Privacy Statement

ResumeForge itself is stateless: it has no database and does not intentionally write the uploaded resume, job description, provider API key, or generated result to disk. The API key is stored by the browser in `localStorage`, sent to the ResumeForge backend in the `X-AI-Key` header for a tailoring request, held only in memory for that request, and forwarded to the selected AI provider.

**The statement “data never leaves your machine” would be inaccurate for this application.** To generate the tailored result, the extracted resume text and target job description are transmitted to the selected external AI provider. Users must review that provider's privacy, retention, and data-processing terms before submitting confidential or sensitive information.

The backend intentionally avoids logging request bodies and API keys. Reverse proxies, hosting platforms, operating systems, browser extensions, or infrastructure outside this repository can still introduce their own logging or telemetry and must be configured separately.

## Important Accuracy Warning

The required AI prompt instructs the model to add numbers, percentages, and dollar amounts where missing. That can cause fabricated metrics that were not present in the original resume. ResumeForge therefore displays a verification warning in the UI. Users must verify every generated number, achievement, company reference, and factual claim before using the output in a job application.

For a production hiring tool, the safer prompt design is to quantify achievements only when supported by the source resume or to mark missing metrics for the user to fill in. This repository preserves the exact prompt requested for this project.

## Security Notes

- Do not put provider API keys in `.env`, Angular source code, Docker images, or source control.
- Provider keys are supplied at runtime through the browser.
- API keys are never returned in API responses.
- PDF uploads are restricted to 10 MB.
- Job descriptions are restricted to 30,000 characters.
- The backend does not implement OCR; scanned PDFs are rejected with a clear error.
- The custom endpoint feature accepts only HTTPS URLs that resolve to public internet addresses.
- Redirects are disabled on outbound provider HTTP requests.
- The custom provider supports Bearer-token OpenAI-compatible endpoints only. Do not accept arbitrary user-defined authentication headers in a public deployment.
- For an internet-facing deployment, add TLS, rate limiting, request-size limits at the edge, observability with secret redaction, a restrictive Content Security Policy, and abuse controls.

## Open-source License

ResumeForge is intended to be distributed under the **GNU Affero General Public License v3.0 (AGPL-3.0)**.

When redistributing or modifying the application, comply with the AGPL v3 requirements, including the network-use source availability obligation. Add the standard AGPL-3.0 license text as `LICENSE` when publishing the repository.

## Contribution Guidelines

1. Fork the repository and create a focused feature branch.
2. Keep backend code compatible with .NET 8 and frontend code compatible with Angular 18 standalone components.
3. Implement provider-specific behavior through `IAiProviderClient` rather than adding provider conditionals to `ResumeService`.
4. Do not introduce a database or persist user resume data unless the project's privacy model is deliberately redesigned and documented.
5. Never commit API keys, sample real resumes, or confidential job application data.
6. Keep provider credentials request-scoped and redact secrets from logs.
7. Run the frontend production build and backend build before opening a pull request.
8. Add or update tests when changing validation, parsing, provider response handling, or export behavior.
9. Keep pull requests small enough to review and include a concise description of behavior changes and security implications.

# AI Provider API Keys & Free/Trial Options

This guide explains how to obtain API keys for AI providers supported by ResumeForge and which providers may offer free, trial, or developer access.

> **Important:** Free tiers, model availability, rate limits, eligibility, and pricing are controlled by the AI providers and can change at any time. Always confirm current terms on the provider's official website.

---

## Recommended Free Starting Options

If you want to try ResumeForge without immediately adding billing details, start by checking:

1. NVIDIA NIM
2. Google Gemini Free Tier
3. Groq Free Plan
4. OpenRouter Free Models

---

## NVIDIA NIM

NVIDIA provides hosted serverless model APIs through NVIDIA Build for development use.

### Get an API key

1. Open https://build.nvidia.com
2. Sign in or create an NVIDIA account.
3. Choose a supported text-generation model.
4. Select **Get API Key** or **Generate API Key**.
5. Copy the generated key.

### Configure ResumeForge

Choose:

```text
Provider:
Other (OpenAI-compatible)

Base URL:
https://integrate.api.nvidia.com/v1

Model:
<model-id-shown-by-NVIDIA>

API Key:
<your-nvidia-api-key>
```

NVIDIA's hosted LLM APIs expose an OpenAI-compatible chat-completions interface.

Official resources:

- https://build.nvidia.com
- https://docs.api.nvidia.com/nim/reference/llm-apis

---

## Google Gemini

Google provides a Gemini API Free Tier for supported models.

### Get an API key

1. Open https://aistudio.google.com
2. Sign in with your Google account.
3. Create or select a project.
4. Generate a Gemini API key.
5. In ResumeForge choose **Google Gemini**.
6. Paste the key and select a model available to your account.

Free-tier availability differs by model.

Official resources:

- https://ai.google.dev/gemini-api/docs/api-key
- https://ai.google.dev/gemini-api/docs/pricing

---

## Groq

Groq provides a Free Plan with model-specific request and token limits.

### Get an API key

1. Open https://console.groq.com
2. Create an account or sign in.
3. Open **API Keys**.
4. Create a new API key.
5. In ResumeForge choose **Groq**.
6. Paste the key and select an available model.

Official resources:

- https://console.groq.com/keys
- https://console.groq.com/docs/rate-limits

---

## OpenRouter

OpenRouter provides access to many models through one API key and also exposes free model options.

### Free model options

You can start with:

```text
openrouter/free
```

Some individual model IDs may also expose a free variant ending in:

```text
:free
```

### Get an API key

1. Open https://openrouter.ai
2. Create an account.
3. Create an API key.
4. In ResumeForge choose **OpenRouter**.
5. Paste the key.
6. Use `openrouter/free` or another model currently available to your account.

Official resources:

- https://openrouter.ai/docs/guides/routing/routers/free-router
- https://openrouter.ai/collections/free-models

---

## OpenAI

ResumeForge supports OpenAI directly.

OpenAI API access may require billing depending on the account, model, and current provider policies.

Official resources:

- https://platform.openai.com/api-keys
- https://platform.openai.com/docs

---

## Anthropic / Claude

ResumeForge supports Anthropic using its native API format.

Trial credits, billing requirements, and model availability may vary.

Official resources:

- https://console.anthropic.com
- https://docs.anthropic.com

---

## xAI / Grok

ResumeForge supports xAI/Grok.

Check the xAI developer console for current API access, model availability, and pricing.

Official resources:

- https://console.x.ai
- https://docs.x.ai

---

## DeepSeek

ResumeForge supports DeepSeek through its OpenAI-compatible API.

Check the official platform for current pricing and account requirements.

Official resources:

- https://platform.deepseek.com
- https://api-docs.deepseek.com

---

## Mistral

ResumeForge supports Mistral.

Check the official developer platform for current model access, free credits, trials, and pricing.

Official resources:

- https://console.mistral.ai
- https://docs.mistral.ai

---

## Custom OpenAI-Compatible Provider

ResumeForge also supports compatible third-party APIs.

Choose:

```text
Provider:
Other (OpenAI-compatible)
```

Then enter:

```text
Base URL:
https://api.example.com/v1

Model:
provider-model-id

API Key:
your-api-key
```

ResumeForge sends the request to:

```text
POST {BASE_URL}/chat/completions
```

### Requirements

The custom provider must:

- support the OpenAI-compatible chat-completions request format
- accept Bearer-token authentication
- expose a public HTTPS endpoint

ResumeForge blocks localhost and private-network destinations for security.

---

## Security Advice

Never:

- commit API keys to Git
- put API keys in committed configuration
- post API keys in GitHub Issues
- include API keys in screenshots
- share provider credentials with contributors

ResumeForge uses a Bring-Your-Own-Key model. API keys should remain under the control of the individual user.

---

## Provider Availability Disclaimer

ResumeForge does not control:

- provider pricing
- free tiers
- trial credits
- rate limits
- model availability
- regional restrictions
- account eligibility

This guide is intended to help users get started. Always verify current information on the provider's official website before relying on a particular free or paid option.

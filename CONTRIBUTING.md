# Contributing to ResumeForge

Thanks for considering a contribution.

ResumeForge is intentionally small, stateless, and provider-agnostic. Contributions should preserve those properties unless a change explicitly proposes a different architecture and documents the trade-offs.

## Development workflow

1. Fork the repository.
2. Create a focused branch from the default branch.
3. Make the smallest change that solves the problem.
4. Build both backend and frontend locally.
5. Add or update tests when the behavior is testable.
6. Open a pull request describing the behavior change, security implications, and any migration impact.

## Backend expectations

- Target .NET 8.
- Keep controllers focused on HTTP concerns and validation.
- Keep resume-tailoring logic outside controllers.
- Add provider-specific behavior through `IAiProviderClient` rather than provider-specific conditionals in `ResumeService`.
- Use async APIs for I/O.
- Do not log API keys, resume text, job descriptions, or full provider payloads.
- Return actionable error messages without exposing secrets or stack traces.
- Preserve request cancellation where practical.

## Frontend expectations

- Use Angular standalone components only.
- Prefer Signals for local reactive state.
- Keep `ChangeDetectionStrategy.OnPush` for application components.
- Do not embed provider API keys in source code or environment files.
- Keep generated documents ATS-conscious and text-selectable.
- Preserve keyboard accessibility and semantic HTML when changing controls.

## Security expectations

Changes affecting custom provider URLs, outbound HTTP, browser credential storage, file uploads, or HTML rendering require explicit security review in the pull request description.

Do not weaken the custom-endpoint SSRF controls to support a provider. Add a dedicated provider adapter instead.

## Privacy expectations

ResumeForge currently has no database and intentionally does not persist user resume data on the server. A contribution that adds accounts, history, analytics, cloud storage, or telemetry must document exactly what data is collected, why, where it is stored, and how users can remove it.

## Pull request checklist

Before opening a pull request, verify that:

- `dotnet build` succeeds in `backend/`.
- `npm install` succeeds in `frontend/`.
- `npm run build` succeeds in `frontend/`.
- No API keys, private resumes, real job applications, or other secrets are committed.
- README/API documentation is updated when public behavior changes.
- Docker configuration still starts both services when infrastructure files change.

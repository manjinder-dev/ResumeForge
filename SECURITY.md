# Security Policy

## Reporting a vulnerability

Please do not publish exploit details for an unpatched vulnerability in a public issue.

If this repository is hosted publicly, report security issues through the repository host's private security-advisory mechanism when available. Include:

- A concise description of the issue
- Affected component and version/commit
- Reproduction steps
- Security impact
- Any suggested mitigation

Do not include real provider API keys, production resumes, or other sensitive user data in the report.

## Security boundaries

ResumeForge is a bring-your-own-key application. Provider API keys are supplied by the user and are not configured as application-owned backend secrets.

The backend intentionally does not persist resume text, job descriptions, generated output, or provider credentials to a database.

The custom AI provider feature is a sensitive boundary because it accepts a user-controlled outbound URL. Its HTTPS-only validation, public-IP checks, DNS pinning, proxy bypass, and redirect restrictions are security controls and should not be removed casually.

## Deployment responsibility

The repository provides application-level controls, but an internet-facing deployment should additionally configure:

- HTTPS/TLS termination
- Rate limiting and abuse controls
- Reverse-proxy request-size limits
- Secret-safe structured logging
- Restrictive Content Security Policy headers
- XSS protections
- Dependency and container-image scanning
- Runtime monitoring and alerting

The browser stores provider keys in `localStorage` for convenience. Any script executing on the same origin can potentially access those values, so preventing XSS is critical.

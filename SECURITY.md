# Security Policy

Thanks for helping keep the Clean Architecture template and the projects built from it secure. This policy explains which versions are supported, how to report a vulnerability and what you can expect in return.

> [!NOTE]
> This policy ships with the Clean Architecture template. If you generated a project from this template, replace the reporting channels below with your own before relying on it.

## Supported versions

The template is distributed from the `main` branch and as an installable NuGet template package. Security fixes target the latest release line and are published as a new package version plus a GitHub security advisory.

| Version | Supported |
| --- | --- |
| Latest release (1.x) | ✅ |
| Older releases | ❌ |

If you are on an older version, upgrade to the latest release first — the issue may already be fixed there.

## Reporting a vulnerability

**Do not open a public issue, discussion or pull request for a security problem.** A public report exposes every project built from the template before a fix exists.

Use one of the private channels below:

| Channel | How |
| --- | --- |
| **GitHub private vulnerability reporting** (preferred) | Repository **Security** tab → **Report a vulnerability**, or open <https://github.com/barisozy/clean-architecture/security/advisories/new> |
| **Email** | <barisozay01@gmail.com> with `SECURITY` in the subject |

GitHub is preferred: the report stays private, and triage, discussion and the eventual advisory all live in one place.

### What a good report contains

- a clear description of the issue and where it lives — project, file, endpoint
- steps to reproduce it, ideally with a minimal proof of concept
- the version, commit or package you tested
- the impact you believe it has and the conditions required to exploit it
- optionally, a suggested fix or mitigation

### What to expect

This is a community project maintained on a best-effort basis, but every report is taken seriously:

| Stage | Target |
| --- | --- |
| Acknowledgement of the report | within 5 business days |
| Initial assessment and severity | within 10 business days |
| Fix and release | as soon as reasonably possible, depending on complexity |
| GitHub advisory | published after the fix, coordinated with you in advance |

You will be credited in the advisory unless you ask to remain anonymous. Please allow a reasonable window for a fix before disclosing the issue publicly — coordinated disclosure protects everyone who builds on the template.

## Scope

This repository contains source code and a NuGet template package; it does not run a hosted service. A report is in scope when it concerns the code in this repository or the published package.

**In scope**

- authentication and session handling — JWT validation, refresh token rotation, reuse detection, token-family revocation
- authorization — permission policies and endpoint protection
- rate limiting, forwarded-header handling and client IP resolution
- input validation, data access through EF Core and information disclosure in error responses or logs
- secrets or credentials committed to the repository
- packaging problems — content that should not ship inside the template package

**Out of scope**

- the documented development-only defaults, such as the local Compose credentials and `dev-only-` JWT secrets. They are bound to `127.0.0.1` and rejected outside the Development environment by design.
- vulnerabilities in third-party dependencies (ASP.NET Core, EF Core, Npgsql, Serilog and so on). Report those upstream; this repository tracks them with Dependabot and applies updates as they become available.
- the template's documented limitations — no email verification, password reset or account lockout; in-process rate limiting and caching; no device binding for refresh tokens. These are known trade-offs of a starter kit; a regular issue is the right place to discuss them.
- social engineering, physical attacks, spam, and anything that only affects your own machine or a misconfigured derived project.

## Safe harbor

Security research conducted in good faith is authorized under this policy. We will not pursue or support legal action against researchers who:

- test only against their own local deployment of the template
- do not access, modify or exfiltrate other people's data or accounts
- do not degrade, interrupt or overload any service
- report findings through the private channels above and allow a reasonable time for a fix before publishing

If you are unsure whether an action is in scope, ask first — a question is cheaper than an incident.

## How this project is kept secure

- Dependencies are tracked with Dependabot, and CI builds and tests every pull request and every push to `main`.
- The build treats warnings as errors and runs a static analyzer (SonarAnalyzer), so risky patterns fail the pipeline rather than shipping.
- Authentication is hardened: HS256-only JWT validation, opaque refresh tokens stored hashed, rotation on every use, reuse detection with token-family revocation and an opt-in grace window for retries.
- Configuration is validated at startup and fails fast; development-only secrets are rejected outside the Development environment.
- Rate limits are partitioned by user or client IP, and forwarded headers are trusted only when known proxies or networks are configured.
- Architecture tests pin layer boundaries and design rules, so structural regressions are caught before review.

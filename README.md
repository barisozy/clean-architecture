# Clean Architecture Template

[![Build](https://github.com/barisozy/clean-architecture/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/barisozy/clean-architecture/actions/workflows/build.yml)
[![NuGet version](https://img.shields.io/nuget/v/Barisozy.Clean.Architecture.Template)](https://www.nuget.org/packages/Barisozy.Clean.Architecture.Template)
[![NuGet downloads](https://img.shields.io/nuget/dt/Barisozy.Clean.Architecture.Template)](https://www.nuget.org/packages/Barisozy.Clean.Architecture.Template)

A **.NET 10** starter for building APIs with clear boundaries between the Domain, Application, Infrastructure, and Web API layers. The template includes working Todo and User use cases, PostgreSQL persistence, authentication, observability, and automated tests.

## Features

### Architecture and application

- **SharedKernel** provides common domain-driven design abstractions.
- **Domain** contains sample entities and domain events.
- **Application** uses lightweight CQRS command/query handlers without MediatR, with Todo and User use cases.
- Logging and validation cross-cutting concerns are implemented with decorators.

### Infrastructure

- JWT authentication with refresh tokens and token rotation.
- Permission-based authorization.
- EF Core with PostgreSQL, snake-case naming, and migrations.
- HybridCache with cache invalidation.
- Structured logging with Serilog and Seq.

### Web API and observability

- Minimal API endpoints, global exception handling, and `ProblemDetails` responses.
- Configurable global and authentication rate limits.
- Scalar/OpenAPI with JWT authorization support.
- Health checks and OpenTelemetry tracing/metrics for ASP.NET Core, HTTP, Npgsql, and the .NET runtime.
- Seq provides a web interface for searching structured logs.

### Tests

- `ArchitectureTests` checks layer boundaries.
- `Application.UnitTests` tests application behavior.
- `IntegrationTests` exercises HTTP endpoints with PostgreSQL through Testcontainers.

## Requirements

- .NET 10 SDK
- Docker Desktop
- Visual Studio 2026, or the .NET CLI

## Run with Docker Compose

From the repository root, run:

```powershell
docker compose up --build
```

| Resource | Address | Local sign-in / connection |
| --- | --- | --- |
| Web API / Scalar API reference | <http://localhost:5000/scalar/v1> | — |
| pgAdmin | <http://localhost:5050> | `admin@local.dev` / `local-dev-only` |
| Seq | <http://localhost:8081> | `admin` / `local-dev-only` |
| PostgreSQL | `localhost:5432` | `postgres` / `postgres` |

In pgAdmin, register a server with host `postgres`, port `5432`, maintenance database `clean-architecture`, username `postgres`, and password `postgres`.

## Run with Aspire

Start `src/Aspire.AppHost` from Visual Studio. Aspire starts the Web API, PostgreSQL, pgAdmin, and Seq with the same local versions, credentials, and database folder used by Compose. The AppHost applies database migrations before the API starts.

Use only one orchestration mode at a time. Both PostgreSQL containers mount `.containers/db`, so they must not access that data directory simultaneously. Stop Compose before starting Aspire, or stop Aspire before starting Compose. Aspire may assign PostgreSQL a dynamic host port; see the resource connection details in the Aspire dashboard.

## Data persistence

PostgreSQL data is stored under `.containers/db` and survives container recreation in either mode. Back up this directory before changing PostgreSQL major versions or deleting it. The local PostgreSQL and Seq credentials are development defaults only; never use them in production.

## Database migrations and production

Compose and Aspire enable EF Core migrations at startup for local development. For production, use a managed or separately operated PostgreSQL service, provide credentials through a secret store or environment variables, and apply migrations as a controlled deployment step before API instances start. Configure production JWT secrets, HTTPS, database TLS, backups, and monitoring.

## Build and test

Run from the repository root:

```powershell
dotnet build CleanArchitecture.slnx
dotnet test CleanArchitecture.slnx
```

Integration tests start a disposable PostgreSQL 18.6 container, so Docker must be running. To target .NET 8 or .NET 9 instead of .NET 10, see the notes in `Directory.Build.props`.

## Use as a .NET project template

Install the template package and generate a new solution:

```powershell
dotnet new install Barisozy.Clean.Architecture.Template
dotnet new cleanarch --name MyCleanArchitecture
```

## Publish the template package to NuGet

The complete solution is published as the `Barisozy.Clean.Architecture.Template` project template. Its version is maintained in the repository-root `Directory.Build.props`. NuGet currently has no package listed with this ID; package IDs are claimed by the first successful publish.

1. Create a NuGet.org API key with package push permissions.
2. In GitHub, add it under **Settings → Secrets and variables → Actions** as `NUGET_API_KEY`.
3. Set or increment `<Version>` in `Directory.Build.props`, commit and push the change, then push a matching `v`-prefixed tag. For version `1.0.0`, run:

   ```powershell
   git tag v1.0.0
   git push origin v1.0.0
   ```

The publish workflow verifies that the tag matches the root version, packs the complete solution as a `dotnet new` template package, and publishes it to NuGet.org. The NuGet badges update after the package is indexed.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup and pull request guidance.

## License

This project is licensed under the [MIT License](LICENSE).

![Clean Architecture Template](https://raw.githubusercontent.com/barisozy/clean-architecture/main/.template.config/icon.png)

# Clean Architecture Template

**A production-minded .NET 10 starter for building HTTP APIs with Clean Architecture.** The layers are already drawn, the dependency rule is enforced by tests, and the boring parts — persistence, authentication, authorization, caching, observability, rate limiting and error handling — are implemented rather than merely referenced.

[![Build](https://github.com/barisozy/clean-architecture/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/barisozy/clean-architecture/actions/workflows/build.yml)
[![Template](https://github.com/barisozy/clean-architecture/actions/workflows/template.yml/badge.svg?branch=main)](https://github.com/barisozy/clean-architecture/actions/workflows/template.yml)
[![NuGet version](https://img.shields.io/nuget/v/Barisozy.CleanArchitecture.Template)](https://www.nuget.org/packages/Barisozy.CleanArchitecture.Template)
[![NuGet downloads](https://img.shields.io/nuget/dt/Barisozy.CleanArchitecture.Template)](https://www.nuget.org/packages/Barisozy.CleanArchitecture.Template)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue)](https://github.com/barisozy/clean-architecture/blob/main/LICENSE)

---

It ships with a working **Todo** and **User** slice, so you start from real, end-to-end code instead of placeholders — entity, command/query handler, validator, EF Core configuration, endpoint and tests already exist for every feature.

## Quick start

**Scaffold a new project** — install the template and generate a ready-to-run solution:

```powershell
dotnet new install Barisozy.CleanArchitecture.Template
dotnet new cleanarch --name MyCleanArchitecture
```

**Run this repository** — start the full local stack with Docker Compose:

```powershell
git clone https://github.com/barisozy/clean-architecture
cd clean-architecture
docker compose up --build
```

The API comes up at <http://localhost:5000/scalar/v1>, with PostgreSQL, pgAdmin and Seq alongside it. [Getting started](#getting-started) covers requirements, the resource addresses and the .NET Aspire alternative.

---

## Table of contents

- [Quick start](#quick-start)
- [Why this template](#why-this-template)
- [Architecture](#architecture)
  - [The dependency rule](#the-dependency-rule)
  - [The layers](#the-layers)
  - [The journey of a request](#the-journey-of-a-request)
  - [One feature across the layers](#one-feature-across-the-layers)
  - [Solution structure](#solution-structure)
- [Features](#features)
- [Tech stack](#tech-stack)
- [Getting started](#getting-started)
  - [Requirements](#requirements)
  - [Run with Docker Compose](#run-with-docker-compose)
  - [Run with Aspire](#run-with-aspire)
- [API endpoints](#api-endpoints)
- [Configuration](#configuration)
  - [Database and migrations](#database-and-migrations)
  - [JWT](#jwt)
  - [Authentication and refresh tokens](#authentication-and-refresh-tokens)
  - [Refresh token cleanup](#refresh-token-cleanup)
  - [Rate limiting and reverse proxies](#rate-limiting-and-reverse-proxies)
  - [Logging](#logging)
  - [Observability](#observability)
- [Testing](#testing)
- [Template packaging and releases](#template-packaging-and-releases)
- [AI agent skills](#ai-agent-skills)
- [Continuous integration](#continuous-integration)
- [Known limitations](#known-limitations)
- [Contributing](#contributing)
- [License](#license)

---

## Why this template

Most Clean Architecture starters stop at the layer diagram. This template carries every concern through to working, tested code:

| Area | What you get |
| --- | --- |
| **Boundaries** | The dependency rule is enforced by architecture tests, together with per-project package allowlists and design rules (sealed entities, internal handlers). |
| **Local orchestration** | Two supported modes — Docker Compose **and** .NET Aspire — sharing the same versions, credentials and data directory. |
| **Authentication** | Opaque, hashed refresh tokens that rotate on every use, detect reuse with a configurable grace window and revoke the whole token family when theft is suspected. |
| **Authorization** | Database-backed, permission-based authorization with a custom policy provider (`HasPermission(...)`), fronted by `HybridCache`. |
| **Resilience** | Global and per-endpoint rate limiting partitioned by authenticated user or client IP, with opt-in, safe handling of forwarded headers. |
| **Observability** | One logging path (Serilog) to stdout as compact JSON, Seq in development and OTLP anywhere; traces and metrics via OpenTelemetry; correlation IDs on every request. |
| **Error handling** | A single `Result` → `ProblemDetails` pipeline with RFC 7807 links — exceptions are reserved for genuinely unexpected failures. |
| **Operational readiness** | Split liveness/readiness probes, a background cleanup job for expired tokens, and warnings-as-errors with a static analyzer in CI. |
| **Testing** | Architecture, unit and full HTTP integration tests (PostgreSQL on Testcontainers) run on every push and pull request. |
| **Packaging** | The repository is also a NuGet template package, verified end to end by CI (pack → install → generate → build → test). |

---

## Architecture

### The dependency rule

Dependencies point **inward**. Nothing references `Infrastructure` except the composition root (`Web.Api`), and the core (`Domain`, `SharedKernel`) has no framework or package dependencies at all.

![Layers and the dependencies between them](https://raw.githubusercontent.com/barisozy/clean-architecture/main/docs/images/dependency-rule.png)

`ArchitectureTests` fail the build if an arrow ever points the wrong way, so the diagram is not just documentation — it is enforced.

### The layers

| Layer | Project | Responsibility | May reference |
| --- | --- | --- | --- |
| **SharedKernel** | `src/SharedKernel` | Framework-free primitives: `Result`, `Error`, `ErrorType`, `Entity`, `IDomainEvent`, `IDomainEventHandler`, `IDateTimeProvider`. | *nothing* |
| **Domain** | `src/Domain` | Entities, domain events and error catalogs. Business rules live here. | `SharedKernel` |
| **Application** | `src/Application` | Use cases as command/query handlers, validators and cross-cutting decorators. Talks to the outside world only through interfaces (`IApplicationDbContext`, `ITokenProvider`, `IUserContext`, …). | `Domain`, `SharedKernel` |
| **Infrastructure** | `src/Infrastructure` | Adapters: EF Core + PostgreSQL, JWT authentication, permission authorization, `HybridCache`, domain-event dispatch, background jobs. | `Application` |
| **Web.Api** | `src/Web.Api` | Minimal API endpoints, middleware, OpenAPI/Scalar and the composition root. | `Infrastructure`, `Aspire.ServiceDefaults` |

### The journey of a request

A request crosses the layers inward and the answer comes back out, each boundary doing exactly one job. No exceptions are used for control flow.

![The journey of a request across the layers](https://raw.githubusercontent.com/barisozy/clean-architecture/main/docs/images/request-journey.png)

### One feature across the layers

Adding a use case touches each layer in its designated place. `CreateTodo` is a complete example:

| Layer | Files | Role |
| --- | --- | --- |
| **Domain** | `src/Domain/Todos/TodoItem.cs` | The entity and its `TodoItemCreatedDomainEvent`. |
| **Application** | `src/Application/Todos/Create/CreateTodoCommand.cs`, `CreateTodoCommandValidator.cs`, `CreateTodoCommandHandler.cs` | The use case: command, FluentValidation validator and handler returning `Result<Guid>`. |
| **Infrastructure** | `src/Infrastructure/Todos/TodoItemConfiguration.cs` | The EF Core mapping, snake_case by convention. |
| **Web.Api** | `src/Web.Api/Endpoints/Todos/Create.cs` | The Minimal API endpoint that receives the request, calls the handler and maps the `Result`. |

Every command and query handler is discovered and decorated automatically — you write the slice, the wiring follows.

### Solution structure

```
clean-architecture/
├─ src/
│  ├─ SharedKernel/           # Result, Error, Entity, IDomainEvent, IDateTimeProvider
│  ├─ Domain/                 # Entities, domain events, error catalogs
│  ├─ Application/            # Use cases: commands, queries, validators, handlers, decorators
│  ├─ Infrastructure/         # EF Core, JWT, permissions, caching, background jobs
│  ├─ Aspire.ServiceDefaults/ # OpenTelemetry, resilience, service discovery
│  ├─ Aspire.AppHost/         # Aspire orchestration for local development
│  └─ Web.Api/                # Minimal API endpoints, middleware, composition root
├─ tests/
│  ├─ ArchitectureTests/      # Layer boundaries and design rules (NetArchTest)
│  ├─ Application.UnitTests/  # Handler and validator tests
│  └─ IntegrationTests/       # HTTP + PostgreSQL through Testcontainers
├─ .agents/skills/            # Agent skills: add-feature, add-entity, add-tests, ca-review
├─ .github/workflows/         # build, template and publish-nuget pipelines
├─ CleanArchitecture.slnx
├─ docker-compose.yml
├─ Directory.Build.props
└─ Directory.Packages.props
```

---

## Features

**Architecture and application**

- **SharedKernel** provides common domain-driven design primitives without pulling in any framework.
- **Domain** contains sample entities and domain events, kept free of infrastructure concerns.
- **Application** uses lightweight CQRS command/query handlers with **no MediatR**, wired with Scrutor.
- Logging and validation cross-cutting concerns are implemented as **plain decorators** you can read in a minute.
- Commands and queries return `Result` / `Result<T>`; there is no exception-based control flow.

**Infrastructure**

- JWT authentication with **opaque, hashed refresh tokens**, rotation on every refresh and reuse detection.
- **Permission-based authorization** backed by the database and cached with `HybridCache`.
- EF Core with PostgreSQL, `snake_case` naming, migrations and a domain-events dispatcher behind `IApplicationDbContext`.
- Structured logging with Serilog (compact JSON to stdout, Seq in development).
- Background cleanup of expired refresh tokens with a configurable retention window.

**Web API and observability**

- Minimal API endpoints discovered by convention, global exception handling and `ProblemDetails` responses.
- Configurable global and authentication rate limits, partitioned by user or client IP.
- Scalar / OpenAPI reference with JWT authorization support.
- Split liveness/readiness health checks and OpenTelemetry traces/metrics for ASP.NET Core, HTTP, Npgsql, EF Core and the .NET runtime.
- Correlation IDs are propagated to logs and echoed in the response so a client report can be traced.

**Tests**

- `ArchitectureTests` verifies layer boundaries, dependency allowlists, design rules and clock usage.
- `Application.UnitTests` tests application behavior.
- `IntegrationTests` exercises real HTTP endpoints against PostgreSQL via Testcontainers.

---

## Tech stack

| Concern | Choice |
| --- | --- |
| Runtime | .NET 10 (works on .NET 8/9 with small edits, see `Directory.Build.props`) |
| Web | ASP.NET Core Minimal APIs, `ProblemDetails`, Scalar / OpenAPI |
| Data | PostgreSQL 18 with EF Core (Npgsql), `snake_case` naming, migrations |
| Messaging | Custom CQRS abstractions + Scrutor decorators (no MediatR) |
| Validation | FluentValidation via a command decorator |
| Auth | JWT bearer (HS256) with rotating, hashed refresh tokens; permission-based authorization |
| Caching | `HybridCache` with cache keys and invalidation |
| Logging | Serilog → stdout (compact JSON), Seq in development, OTLP when configured |
| Telemetry | OpenTelemetry (traces + metrics) via `Aspire.ServiceDefaults` |
| Local orchestration | Docker Compose *or* .NET Aspire |
| Testing | xUnit, Shouldly, NSubstitute, NetArchTest, Testcontainers |

---

## Getting started

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) or another running Docker engine — used by Compose, Aspire and the integration tests
- Visual Studio 2026 or the .NET CLI

### Run with Docker Compose

From the repository root:

```powershell
docker compose up --build
```

| Resource | Address | Local sign-in / connection |
| --- | --- | --- |
| Web API / Scalar API reference | <http://localhost:5000/scalar/v1> | — |
| pgAdmin | <http://localhost:5050> | `admin@local.dev` / `local-dev-only` |
| Seq | <http://localhost:8081> | `admin` / `local-dev-only` |
| PostgreSQL | `localhost:5432` | `postgres` / `postgres` |

All published ports bind to `127.0.0.1`, so the stack is reachable only from the local machine. To expose a service on your network, change the binding deliberately; the local credentials are development defaults.

In pgAdmin, register a server with host `postgres`, port `5432`, maintenance database `clean-architecture`, username `postgres` and password `postgres`.

### Run with Aspire

Start `src/Aspire.AppHost` from Visual Studio. Aspire starts the Web API, PostgreSQL, pgAdmin and Seq with the same versions, credentials and data folder used by Compose. The AppHost applies database migrations before the API starts.

> [!WARNING]
> Run only one orchestration mode at a time. Both PostgreSQL containers mount `.containers/db`, so they must not access that data directory simultaneously — stop Compose before starting Aspire, or the reverse.

Aspire may assign PostgreSQL a dynamic host port; see the resource connection details in the Aspire dashboard.

---

## API endpoints

The two working slices — **Users** and **Todos** — cover the full path from HTTP request to database.

**Users** (`Tags.Users`)

| Method | Route | Authorization | Rate limit |
| --- | --- | --- | --- |
| `POST` | `/users/register` | Anonymous | `authentication` |
| `POST` | `/users/login` | Anonymous | `authentication` |
| `POST` | `/users/refresh-token` | Anonymous | `authentication` |
| `POST` | `/users/logout` | Bearer token | `authentication` |
| `GET` | `/users/{userId}` | `users:access` permission | Global |

**Todos** (`Tags.Todos`) — all require a bearer token

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/todos?userId={guid}` | List a user's todos |
| `GET` | `/todos/{id:guid}` | Get a todo by id |
| `POST` | `/todos` | Create a todo |
| `PUT` | `/todos/{id:guid}` | Update a todo |
| `PUT` | `/todos/{id:guid}/complete` | Mark a todo complete |
| `POST` | `/todos/{todoId:guid}/copy` | Copy a todo |
| `DELETE` | `/todos/{id:guid}` | Delete a todo |

**Health**

| Endpoint | Purpose | Notes |
| --- | --- | --- |
| `/health/live` | Liveness probe | Process responsiveness only; never touches the database |
| `/health/ready` | Readiness probe | Checks the database; the body is the status only |
| `/health` | Detailed report | Development only |

Probe endpoints are exempt from rate limiting and expose no check names or exception details.

---

## Configuration

### Database and migrations

Compose and Aspire enable EF Core migrations at startup for local development (`Database:ApplyMigrationsOnStartup`).

> [!WARNING]
> PostgreSQL data is stored under `.containers/db` and survives container recreation in either mode. Back this directory up before changing the PostgreSQL major version or deleting it.

For production, use a managed or separately operated PostgreSQL service, provide credentials through a secret store or environment variables, and apply migrations as a controlled deployment step before API instances start. Configure production JWT secrets, HTTPS, database TLS, backups and monitoring.

### JWT

The application validates its configuration at startup and fails fast if the JWT settings are invalid. Provide them through a secret store or environment variables:

```text
Jwt__Secret=<random, at least 32 bytes>
Jwt__Issuer=<issuer>
Jwt__Audience=<audience>
Jwt__ExpirationInMinutes=60
```

> [!IMPORTANT]
> Secrets starting with `dev-only-` are rejected outside the Development environment.

Tokens are signed and validated with HS256 only.

### Authentication and refresh tokens

Emails are stored trimmed and lowercased, and sign-in and registration treat addresses case-insensitively, so `User@Example.com` and `user@example.com` are the same account. Registering an address that differs only in case from an existing one returns `409 Conflict`, invalid credentials return `401 Unauthorized`, and requests with missing or malformed credentials are rejected with `400 Bad Request` before any password verification.

Every refresh rotates the token: the old token is marked as used and a new one is issued for the same family. Presenting an already-used token revokes the whole family, because the token may have been stolen. A client retry or a second tab can replay a just-rotated token, so `RefreshTokenReuse:ReuseGraceSeconds` (default `0`, strict) can be set to a small window (for example `10`) in which a replayed token is still rejected but the family is not revoked.

Logout (`POST users/logout` with the refresh token) requires an authenticated request and revokes the caller's token family; access tokens stay valid until they expire. Tokens that are unknown or belong to another user are ignored with `204 No Content`, so the endpoint is idempotent and reveals nothing.

![Refresh token rotation and reuse handling](https://raw.githubusercontent.com/barisozy/clean-architecture/main/docs/images/refresh-token-rotation.png)

### Refresh token cleanup

A background service deletes refresh tokens that expired more than `RefreshTokenCleanup:RetentionInDays` (default `30`) days ago, once every `RefreshTokenCleanup:IntervalInMinutes` (default `60`) minutes. Expired tokens are kept for the retention period as an audit trail. The deletion is idempotent, so running several API instances is safe.

### Rate limiting and reverse proxies

Rate limits are partitioned by authenticated user (`sub` claim) and otherwise by client IP. Behind a reverse proxy or load balancer, enable forwarded headers and list the proxies you trust:

```text
ForwardedHeaders__Enabled=true
ForwardedHeaders__KnownProxies__0=10.0.0.5
ForwardedHeaders__KnownNetworks__0=10.0.0.0/8
```

> [!WARNING]
> Forwarded headers are ignored by default. Enabling them without at least one known proxy or network fails at startup, because trusting unknown peers would let clients spoof their address.

### Logging

Logs are written to stdout as compact JSON (one event per line) in every environment. Override levels or add sinks through configuration or environment variables, for example `Serilog__MinimumLevel__Default=Warning`. Development additionally writes to Seq. Each request log carries the correlation ID and the authenticated user id (`UserId`), so logs and traces can be correlated.

### Observability

| Signal | Path |
| --- | --- |
| Traces and metrics | OpenTelemetry SDK (`Aspire.ServiceDefaults`) |
| Logs | Serilog: stdout (compact JSON), Seq in Development, OTLP when configured |

Set the standard OpenTelemetry variables to export to any OTLP backend:

```text
OTEL_EXPORTER_OTLP_ENDPOINT=http://collector:4317
OTEL_EXPORTER_OTLP_PROTOCOL=grpc            # or http/protobuf
OTEL_SERVICE_NAME=web-api
OTEL_RESOURCE_ATTRIBUTES=service.version=1.0.0
```

Port `4317` is the conventional OTLP/gRPC port and `4318` the OTLP/HTTP one. With `http/protobuf`, point the endpoint at the collector's base URL — the per-signal path (`/v1/logs`, `/v1/traces`, `/v1/metrics`) is appended automatically.

Expected business outcomes (validation, not found, conflict) are logged as information, forbidden as warning, and only unexpected failures as errors.

---

## Testing

Build and run the full suite from the repository root:

```powershell
dotnet build CleanArchitecture.slnx
dotnet test CleanArchitecture.slnx
```

> [!NOTE]
> Integration tests start a disposable PostgreSQL 18 container, so Docker must be running.

| Project | Focus |
| --- | --- |
| `tests/ArchitectureTests` | Enforces layer boundaries, per-project dependency allowlists, design rules (sealed entities, internal handlers) and clock usage. |
| `tests/Application.UnitTests` | Handler and validator behavior with in-memory substitutes. |
| `tests/IntegrationTests` | Real HTTP endpoints against PostgreSQL via Testcontainers. |

To target .NET 8 or .NET 9 instead of .NET 10, follow the notes in `Directory.Build.props`.

---

## Template packaging and releases

The repository doubles as a NuGet template package. The `Template` workflow packs the template, generates a solution from the packed package and runs the generated tests, so every published package is verified end to end. Releases are published to NuGet when a `v*.*.*` tag matching the package version is pushed. For usage, see [Quick start](#quick-start).

## AI agent skills

The repository ships an agent skill pack under `.agents/skills/` that teaches an AI coding agent the conventions of the template, so generated code follows the same patterns:

| Skill | Invoke with | What it does |
| --- | --- | --- |
| **add-feature** | `/add-feature archive a todo item` | Scaffolds a complete vertical slice: command/query, handler, validator, endpoint and tests. |
| **add-entity** | `/add-entity Project with a name and owner` | Adds a domain entity end to end: entity, error catalog, domain events, EF configuration, migration. |
| **add-tests** | `/add-tests CopyTodoCommand` | Backfills handler, validator and integration tests for existing use cases. |
| **ca-review** | `/ca-review` | Reviews pending changes against the template's conventions. |

See `.agents/skills/README.md` for details. The skills are plain Markdown, so you can tailor them to your team's conventions.

## Continuous integration

| Workflow | Trigger | Purpose |
| --- | --- | --- |
| `build.yml` | Push to `main`, pull requests | Restore, build, test and publish the solution, then build the Docker image and run a Compose smoke test against `/health/ready`. |
| `template.yml` | Push to `main`, pull requests | Packs the NuGet template, generates a solution from it and runs its tests. |
| `publish-nuget.yml` | `v*.*.*` tags | Verifies the tag matches the package version and publishes to NuGet.org. |

---

## Known limitations

This template is a starting point rather than a finished product:

- There is no email verification, password reset or account lockout flow.
- Rate limiting and `HybridCache` use in-process state; a multi-instance deployment should back them with a shared store (for example Redis).
- Refresh tokens are opaque, stored hashed and revoked by family, but they are not bound to a device fingerprint.
- The API only exposes register, login, refresh, logout and get-user endpoints; there is no user administration.

## Contributing

See [CONTRIBUTING.md](https://github.com/barisozy/clean-architecture/blob/main/CONTRIBUTING.md) for development setup and pull request guidance.

## License

This project is licensed under the [MIT License](https://github.com/barisozy/clean-architecture/blob/main/LICENSE).
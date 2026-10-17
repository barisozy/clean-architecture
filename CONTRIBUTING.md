# Contributing

Thanks for your interest in improving the Clean Architecture template. This guide covers development setup, the conventions every change is expected to follow, and how to get a pull request merged.

By taking part in this project you agree to follow the [Code of Conduct](CODE_OF_CONDUCT.md). Please report security vulnerabilities through the private channels in [SECURITY.md](SECURITY.md) — never in a public issue.

## Ways to contribute

| How | Start with |
| --- | --- |
| Report a bug | An issue describing what you expected, what happened and how to reproduce it |
| Propose a feature or design change | An issue describing the problem; for anything larger than a small fix, agree on the approach before writing code |
| Improve the documentation or agent skills | A focused pull request — `README.md`, `docs/` and `.agents/skills/` are as important as the code |
| Submit code | Small, focused pull requests are the easiest to review and merge |

## Development setup

Prerequisites:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) or another running Docker engine — integration tests, Docker Compose and .NET Aspire all need it
- Visual Studio 2026, VS Code or JetBrains Rider — an editor that understands .NET solutions

### Build and test

From the repository root:

```powershell
dotnet build CleanArchitecture.slnx
dotnet test CleanArchitecture.slnx
```

The build treats warnings as errors and runs static analysis, so it only succeeds on a clean tree. The test run covers three projects:

| Project | Focus |
| --- | --- |
| `tests/ArchitectureTests` | Layer boundaries, dependency allowlists and design rules — no Docker required |
| `tests/Application.UnitTests` | Handler and validator behavior with in-memory substitutes — no Docker required |
| `tests/IntegrationTests` | Real HTTP endpoints against PostgreSQL 18 on Testcontainers — **Docker must be running** |

While iterating, narrow the run:

```powershell
dotnet test tests/Application.UnitTests
dotnet test CleanArchitecture.slnx --filter "FullyQualifiedName~CreateTodoCommand"
```

### Run the stack

```powershell
docker compose up --build
```

The API comes up at <http://localhost:5000/scalar/v1>, with PostgreSQL, pgAdmin and Seq alongside it; `docker-compose.yml` holds the ports and local credentials. Starting `src/Aspire.AppHost` is the supported Aspire alternative.

> [!WARNING]
> Run only one orchestration mode at a time — Compose and Aspire both mount `.containers/db`.

## Conventions

The template exists to model a way of structuring code, so consistency matters as much as correctness. The `.agents/skills/ca-review` skill documents the same rules and can review a diff for you; this section is the manual version.

### Architecture

Dependencies point inward — `Web.Api → Infrastructure → Application → Domain → SharedKernel` — and `ArchitectureTests` fails the build when an arrow points the wrong way or a package leaves its layer.

- Add project references and packages only where they belong; update an allowlist only when the dependency genuinely belongs to that layer.
- `SharedKernel` and `Domain` stay framework-free.
- Persistence details (keys, conversions, relationships) live in Infrastructure `IEntityTypeConfiguration<>` classes, not on entities.

### Use case slices

- One folder per use case under `src/Application/{Feature}/{UseCase}/`; the endpoint mirrors it at `src/Web.Api/Endpoints/{Feature}/{UseCase}.cs`.
- Naming: `{Verb}{Entity}Command`, `Get{X}Query`, `...Handler`, `...Validator`, `{X}Response`.
- Handlers and validators are `internal sealed` and discovered automatically — no MediatR, no manual DI registration.
- Response DTOs are per-slice; queries project with `.Select(...)` and never return domain entities.
- Endpoints map the request to a command or query and the `Result` to an HTTP response — no business logic.

### Errors and validation

- Expected failures return `Result`/`Result<T>`; exceptions are reserved for genuinely unexpected failures.
- Errors come from static `{Entity}Errors` factories with `"{Feature}.{Reason}"` codes and the semantically correct type (`NotFound`, `Conflict`, `Forbidden`, …).
- Every command has a FluentValidation `{Command}Validator`; handlers enforce business rules but do not re-check input shape.
- Handlers acting on user-owned data filter by `IUserContext.UserId` or return a forbidden error.
- No `DateTime.UtcNow` or `DateTime.Now` in Application — use `IDateTimeProvider`.

### State changes

- Commands raise a domain event with `entity.Raise(new XDomainEvent(id))` before `SaveChangesAsync`.
- Every `HybridCache`-cached read has matching invalidation (`cache.RemoveAsync`) in each command that mutates that data.

### Tests

A change is complete when:

- every handler has unit tests covering the happy path and each `Result.Failure` path, including persisted state and raised events
- validators have per-rule tests written with `TestValidate`
- new or changed endpoints have integration tests over real HTTP
- naming and Arrange/Act/Assert structure match the existing tests

### Code style

`.editorconfig` is the source of truth and is enforced at build time — file-scoped namespaces, no `this.` qualification, predefined type keywords and explicit braces are examples of what the compiler checks. Running `dotnet build` before pushing catches almost everything.

### Agent skills

The skills under `.agents/skills/` scaffold and review code that follows these conventions:

| Skill | Invoke with | Purpose |
| --- | --- | --- |
| **add-feature** | `/add-feature archive a todo item` | A complete vertical slice: command or query, handler, validator, endpoint and tests |
| **add-entity** | `/add-entity Project with a name and owner` | A domain entity end to end: entity, errors, events, EF configuration, migration |
| **add-tests** | `/add-tests CopyTodoCommand` | Backfills handler, validator and integration tests |
| **ca-review** | `/ca-review` | Reviews pending changes against the conventions above |

They are plain Markdown, so you can generate the same code by hand — the skills simply describe exactly what "done" means.

## Commits and branches

Branch names follow `<type>/<short-description>` using the commit types below — for example `feat/db-backed-permissions` or `docs/readme-refresh`.

Commits follow [Conventional Commits](https://www.conventionalcommits.org/):

```text
<type>[optional scope]: <description>
```

| Type | Use for |
| --- | --- |
| `feat` | A new capability or behavior |
| `fix` | A bug fix |
| `docs` | Documentation only |
| `test` | Tests only |
| `refactor` | A behavior-preserving restructure |
| `chore` | Build, tooling, dependencies and housekeeping |

Keep the subject short and imperative (`add refresh token grace window`, not `Added …`), explain the *why* in the body when it is not obvious, and reference issues with `Fixes #123`.

## Pull requests

Before opening a pull request:

- [ ] the change is focused on a single concern and the diff is as small as the task allows
- [ ] `dotnet build CleanArchitecture.slnx` succeeds without warnings
- [ ] `dotnet test CleanArchitecture.slnx` passes locally with Docker running
- [ ] new or changed behavior is covered by tests, and documentation is updated where it matters
- [ ] no secrets, real credentials, local database files (`.containers/`) or build output (`bin/`, `obj/`, `TestResults/`) are included

In the description, state what changed, why, how you verified it and which issue it closes. The `Build` and `Template` pipelines run the same build and tests, so they should be green before review. A maintainer reviews every pull request and may ask for adjustments — reviews target the code, not the coder, and a round of feedback is normal.

## Working on the template itself

This repository is also packaged as a NuGet template, so changes to `.template.config/`, `CleanArchitecture.Template.csproj` or to any file that ships inside the package deserve an end-to-end check:

```powershell
dotnet pack CleanArchitecture.Template.csproj --configuration Release --output artifacts

# Install the package you just built — --force replaces any previously installed copy:
$package = Get-ChildItem artifacts\*.nupkg | Sort-Object LastWriteTime -Descending | Select-Object -First 1
dotnet new install $package.FullName --force

dotnet new cleanarch --name SmokeTest --output smoke-test
cd smoke-test
dotnet build
dotnet test
```

Keep repository-only material out of the package: `README.md`, `PACKAGE-README.md`, `docs/` and the release workflows are excluded on purpose. Leave version bumps and `v*.*.*` tags to the maintainer; releases are cut from `main` when the version changes.

## License

This project is licensed under the [MIT License](LICENSE). By contributing you agree that your contributions are provided under the same license.

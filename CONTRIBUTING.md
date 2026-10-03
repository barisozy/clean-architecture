# Contributing

Contributions are welcome. For a substantial change, open an issue first to discuss the proposed approach. Keep pull requests focused and explain what changed and why.

## Development setup

- .NET 10 SDK
- Docker Desktop or another running Docker engine (required for integration tests)

Build and run the test suite from the repository root:

```powershell
dotnet build CleanArchitecture.slnx
dotnet test CleanArchitecture.slnx
```

## Pull requests

- Follow the existing project structure, architecture boundaries, and `.editorconfig` conventions.
- Add or update tests for behavior changes. Integration tests use Testcontainers and require Docker.
- Run the build and tests before submitting, and include the results in the pull request description.
- Do not include credentials, secrets, local database files, or generated build output.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

IDistributedApplicationBuilder builder =
    DistributedApplication.CreateBuilder(args);

IResourceBuilder<ParameterResource> postgresPassword =
    builder.AddParameter("postgres-password", value: "postgres", secret: true);

IResourceBuilder<PostgresServerResource> postgres =
    builder
        .AddPostgres("postgres", password: postgresPassword)
        .WithImage("postgres", "18.6")
        .WithEnvironment("POSTGRES_DB", "clean-architecture")
        .WithBindMount("../../.containers/db", "/var/lib/postgresql");

IResourceBuilder<PostgresDatabaseResource> database =
    postgres.AddDatabase("Database", "clean-architecture");

postgres.WithPgAdmin(pgAdmin => pgAdmin
    .WithImage("dpage/pgadmin4", "9.18.0")
    .WithEnvironment("PGADMIN_DEFAULT_EMAIL", "admin@local.dev")
    .WithEnvironment("PGADMIN_DEFAULT_PASSWORD", "local-dev-only")
    .WithHostPort(5050));

IResourceBuilder<ContainerResource> seq =
    builder
        .AddContainer("seq", "datalust/seq", "2026.1")
        .WithEnvironment("ACCEPT_EULA", "Y")
        .WithEnvironment("SEQ_FIRSTRUN_ADMINPASSWORD", "local-dev-only")
        .WithHttpEndpoint(port: 8081, targetPort: 80, name: "ui")
        .WithHttpEndpoint(port: 5341, targetPort: 5341, name: "ingest");

builder.AddProject<Projects.Web_Api>("web-api")
    .WithReference(database)
    .WithEnvironment("Serilog__WriteTo__1__Args__ServerUrl", seq.GetEndpoint("ingest"))
    .WithEnvironment("Database__ApplyMigrationsOnStartup", "true")
    .WaitFor(database)
    .WaitFor(seq);

await builder.Build().RunAsync();

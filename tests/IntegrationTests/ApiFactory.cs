using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Web.Api;

namespace IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Database-less tests never connect; the value only has to exist so startup does not
        // depend on user secrets, environment variables or test execution order.
        builder.UseSetting(
            "ConnectionStrings:Database",
            "Host=127.0.0.1;Port=1;Database=unused;Username=u;Password=p;Timeout=1");
    }
}

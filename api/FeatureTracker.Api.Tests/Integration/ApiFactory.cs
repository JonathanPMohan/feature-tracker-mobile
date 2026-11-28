using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FeatureTracker.Api.Tests.Integration;

/// <summary>
/// Runs the real API in memory for tests (B12): the real <c>Program.cs</c>,
/// middleware and endpoints, with one thing swapped: the database it talks to.
/// </summary>
/// <remarks>
/// <para>
/// <c>WebApplicationFactory&lt;Program&gt;</c> starts the app inside the test
/// process. <c>CreateClient()</c> returns an <c>HttpClient</c> whose requests
/// go straight into the app, with no network and no port. They still pass
/// through routing, middleware and serialization exactly as a real request
/// would, which is what makes these integration tests and not unit tests.
/// </para>
/// <para>
/// The Next.js comparison: your M5 action tests called the server action as
/// a function with a faked session. This goes one layer further out: a real
/// HTTP request to a real endpoint, the same way the phone will call it.
/// </para>
/// <para>
/// <c>Program</c> is the class the compiler generates for <c>Program.cs</c>'s
/// top-level statements. In .NET 10 it's public, so the tests can name it.
/// </para>
/// </remarks>
public sealed class ApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    // Called while the app is being built, before Program.cs's code reads
    // its configuration.
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing", not "Development": appsettings.Development.json (your
        // Docker database, SQL logging) isn't loaded, so a test can never
        // touch your dev data by accident.
        builder.UseEnvironment("Testing");

        // Adds ConnectionStrings:Default to the app's configuration, with
        // higher priority than the appsettings files. Program.cs reads it
        // when the first AppDbContext is created.
        builder.UseSetting("ConnectionStrings:Default", connectionString);
    }
}

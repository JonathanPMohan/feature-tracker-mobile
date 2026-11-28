// Program.cs: the entry point of the API, the code that runs on `dotnet run`.
//
// It uses "top-level statements": plain code at the top of the file, with no
// `class Program { static void Main() { ... } }` wrapper around it. The
// compiler adds that wrapper for you. Only one file per project may do this.
//
// The file has two phases, and the order matters:
//   1. builder: register services (configuration, logging, later the
//      database and auth), before the app exists.
//   2. app: describe how each HTTP request is handled (middleware and
//      endpoints), then start listening.
// In Next.js, the framework did both for you from the folder structure. Here
// they're explicit, which is why every new feature starts with a line here.

// ApiInfo lives in the FeatureTracker.Api namespace (ApiInfo.cs). This file
// can't declare a namespace, so it imports that one.
using FeatureTracker.Api;
using FeatureTracker.Api.Data;
using Microsoft.EntityFrameworkCore;

// ---------- 1. Build: register services ----------

// CreateBuilder reads configuration from appsettings.json, then
// appsettings.{Environment}.json, then environment variables, then the
// command line (later sources win). It also sets up logging to the console.
// `args` are the command-line arguments, available here automatically.
var builder = WebApplication.CreateBuilder(args);

// The database (M2, B2–B4). AddDbContext registers AppDbContext as a scoped
// service: one instance per HTTP request, handed to any endpoint that lists
// it as a parameter (primer, section 7).
//
// The lambda runs when an AppDbContext is first needed, not now. So the
// connection string is read late, after every configuration source is
// loaded, including the one the integration tests add to point at their
// Testcontainers database (B12). `sp` is the service provider: the container
// of registered services, from which IConfiguration is read.
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    // ConnectionStrings:Default, from appsettings.Development.json on your
    // Mac (decision B) or from the tests. GetConnectionString returns null
    // if it's missing, and `??` turns that into a clear error instead of a
    // confusing one deep inside Npgsql.
    var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")
        ?? throw new InvalidOperationException(
            "Missing connection string 'ConnectionStrings:Default'. " +
            "In development it's in appsettings.Development.json.");

    options
        .UseNpgsql(connectionString) // Postgres, through Npgsql
        .UseSnakeCaseNamingConvention(); // FeatureRequest.AuthorId → feature_request.author_id (decision C)
});

// Health checks (B7–B9): ASP.NET Core's built-in system. AddDbContextCheck
// asks EF Core "can you connect?" (it runs a trivial query).
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

// (M3 adds auth here.)

var app = builder.Build();

// ---------- 2. Handle requests: endpoints ----------

// A Minimal API endpoint: an HTTP verb, a route, and a function.
// The Next.js comparison: a route handler at app/route.ts exporting GET().
// Not a server action: this has its own URL and verb, and any HTTP client
// (the phone, curl, a browser) can call it.
//
// The function returns a C# object, and ASP.NET Core serializes it to JSON
// with property names in camelCase: ApiInfo.Name becomes "name". So the
// response is  {"name":"Feature Tracker API"}  with Content-Type
// application/json, and status 200.
app.MapGet("/", () => new ApiInfo("Feature Tracker API"));

// GET /health (B7–B9): 200 "Healthy" when every registered check passes,
// 503 "Unhealthy" when one fails. The default response is just that one
// word, on purpose: no connection string, host or error message, because
// this endpoint is public (B9). The details go to the server's log instead.
app.MapHealthChecks("/health");

// Start the web server and block until it's stopped (Ctrl+C). The address
// comes from Properties/launchSettings.json in development: 0.0.0.0:5080,
// meaning every network interface, not only localhost, so the phone on your
// Wi-Fi can reach it (B14).
app.Run();

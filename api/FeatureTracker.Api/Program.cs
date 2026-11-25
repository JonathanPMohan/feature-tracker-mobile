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

// ---------- 1. Build: register services ----------

// CreateBuilder reads configuration from appsettings.json, then
// appsettings.{Environment}.json, then environment variables, then the
// command line (later sources win). It also sets up logging to the console.
// `args` are the command-line arguments, available here automatically.
var builder = WebApplication.CreateBuilder(args);

// (M2 adds the database here, M3 adds auth: builder.Services.Add...)

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

// Start the web server and block until it's stopped (Ctrl+C). The address
// comes from Properties/launchSettings.json in development: localhost:5080.
app.Run();

// ---------- Types ----------

/// <summary>The response of <c>GET /</c>.</summary>
/// <remarks>
/// A <c>record</c> is a class made for holding data: this one line declares
/// a type with a read-only <c>Name</c> property and a constructor, and two
/// records with the same values are equal. The TypeScript equivalent is
/// <c>type ApiInfo = { readonly name: string }</c>. Types in this file must
/// come after the top-level statements.
/// </remarks>
internal sealed record ApiInfo(string Name);

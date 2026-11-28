using FeatureTracker.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace FeatureTracker.Api.Tests.Integration;

/// <summary>
/// One throwaway Postgres for the whole test run (B11), with the migrations
/// applied, and one <see cref="ApiFactory"/> pointing at it.
/// </summary>
/// <remarks>
/// <para>
/// Testcontainers starts a real Docker container from the tests themselves:
/// the same <c>postgres:18</c> image as docker-compose.yml (decision A), on a
/// random free port, with a random password. When the test run ends, it's
/// stopped and deleted. Your dev database is never touched.
/// </para>
/// <para>
/// <c>IAsyncLifetime</c> gives xUnit two async hooks: <c>InitializeAsync</c>
/// before the first test that uses the fixture, and <c>DisposeAsync</c> after
/// the last. It's Vitest's <c>beforeAll</c>/<c>afterAll</c> as a class.
/// </para>
/// </remarks>
public sealed class PostgresFixture : IAsyncLifetime
{
    // The image tag lives in one place, so tests and docker-compose.yml are
    // easy to keep in sync.
    public const string PostgresImage = "postgres:18";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(PostgresImage).Build();

    // Null until InitializeAsync has started the container. `ApiFactory?`
    // says so honestly, instead of pretending with `= null!`.
    private ApiFactory? _factory;

    // Tests use this. `??` + throw: if setup failed and a test still asks,
    // it gets a message that names the real cause instead of a
    // NullReferenceException. `=>` makes it a computed property: the
    // expression runs on every read, like a getter in TypeScript.
    public ApiFactory Factory =>
        _factory ?? throw new InvalidOperationException(
            "The Postgres test container didn't start. Is Docker running? See the first error in the test output.");

    public async Task InitializeAsync()
    {
        // Pulls the image if needed (only the first time on a machine), starts
        // the container, and waits until Postgres accepts connections.
        // If Docker isn't running, Testcontainers fails even earlier, while
        // building the container (the field above), with
        // DockerUnavailableException: "Failed to connect to Docker endpoint".
        await _container.StartAsync();

        _factory = new ApiFactory(_container.GetConnectionString());

        // Apply every migration, the same way `dotnet ef database update`
        // does on your Mac. The AppDbContext comes from the app's own
        // services, so it's configured exactly like the real API's (Npgsql,
        // snake_case), with no second copy of that setup to drift.
        // A "scope" is a mini-request: scoped services like AppDbContext live
        // inside one, and `using` disposes it at the end of the block.
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        // xUnit calls this even when InitializeAsync failed part way. Only
        // dispose what was created, so cleanup never adds a second error on
        // top of the real one. (Found by /code-review on PR #4.)
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _container.DisposeAsync(); // stops and deletes the container
    }
}

/// <summary>
/// Puts every test class marked <c>[Collection(PostgresCollection.Name)]</c>
/// in one group that shares a single <see cref="PostgresFixture"/>.
/// </summary>
/// <remarks>
/// xUnit 2 has no "once for the whole run" fixture, so a collection fixture
/// is the way to share one container between test classes. Tests in one
/// collection run one at a time, never in parallel, which also keeps
/// database tests from stepping on each other.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}

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

    // `= null!` says "this starts null, but InitializeAsync sets it before
    // any test runs". The `!` is the nullable escape hatch from the primer,
    // used here because xUnit guarantees the order.
    public ApiFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Pulls the image if needed (only the first time on a machine), starts
        // the container, and waits until Postgres accepts connections.
        await _container.StartAsync();

        Factory = new ApiFactory(_container.GetConnectionString());

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
        await Factory.DisposeAsync();
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

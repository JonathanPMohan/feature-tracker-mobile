using FeatureTracker.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FeatureTracker.Api.Tests.Integration;

// The migration workflow: rule B5, plus a guard that matters from M4 on.
[Collection(PostgresCollection.Name)]
public sealed class MigrationTests(PostgresFixture fixture)
{
    [Fact]
    public async Task The_initial_migration_has_been_applied() // B5
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // EF Core reads its __EFMigrationsHistory table: one row per applied
        // migration, named "<timestamp>_<Name>", e.g. 20261127093000_Initial.
        var applied = await db.Database.GetAppliedMigrationsAsync();

        // Assert.Contains with a condition: passes if any item matches.
        Assert.Contains(applied, id => id.EndsWith("_Initial", StringComparison.Ordinal));
    }

    [Fact]
    public async Task No_migration_is_waiting_to_be_applied()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Every migration in the code has been applied to the database.
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public void The_model_matches_the_migrations()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // The guard for M4 onwards. HasPendingModelChanges compares the C#
        // model (your entities and their configuration) with the latest
        // migration's snapshot. If someone changes an entity and forgets
        // `dotnet ef migrations add`, this fails, in CI, before the change
        // can merge. Without it, the app would run against tables that don't
        // match the code. The Drizzle version of this mistake: editing
        // schema.ts and never running drizzle-kit generate.
        Assert.False(
            db.Database.HasPendingModelChanges(),
            "The model has changes with no migration. Run: " +
            "dotnet ef migrations add <Name> --project api/FeatureTracker.Api --output-dir Data/Migrations");
    }
}

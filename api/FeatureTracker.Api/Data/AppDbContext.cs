using Microsoft.EntityFrameworkCore;

namespace FeatureTracker.Api.Data;

/// <summary>
/// The API's connection to the database: EF Core's "unit of work". Every
/// query and save goes through an instance of this class.
/// </summary>
/// <remarks>
/// <para>
/// The Drizzle comparison: this is your <c>db</c> object plus your schema in
/// one place. Each table becomes a <c>DbSet&lt;T&gt;</c> property, and
/// LINQ on that property becomes SQL (primer, section 5).
/// </para>
/// <para>
/// It's empty on purpose. The tables are your data model, in M4. Even empty,
/// it's enough for the migration workflow (<c>dotnet ef migrations add</c>)
/// and the database health check.
/// </para>
/// <para>
/// <c>(DbContextOptions&lt;AppDbContext&gt; options) : DbContext(options)</c>
/// is a primary constructor that passes its argument straight to the base
/// class. The options say which database and provider to use; they're built
/// in <c>Program.cs</c> (<c>AddDbContext</c>), so this class never sees a
/// connection string. One <c>AppDbContext</c> is created per HTTP request
/// (a "scoped" service, primer section 7) and disposed after it.
/// </para>
/// </remarks>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // M4 adds the tables here, for example:
    //   public DbSet<FeatureRequest> FeatureRequests => Set<FeatureRequest>();
    // and their configuration in OnModelCreating.
}

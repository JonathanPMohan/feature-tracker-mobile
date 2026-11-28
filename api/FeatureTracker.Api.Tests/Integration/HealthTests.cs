using System.Net;

namespace FeatureTracker.Api.Tests.Integration;

// GET /health: rules B7, B8 and B9.
[Collection(PostgresCollection.Name)]
public sealed class HealthTests(PostgresFixture fixture)
{
    // An address where nothing listens: port 1 on this machine. Connecting
    // fails at once ("connection refused"), which is how the API sees a
    // stopped database. The credentials are fake on purpose, so the test
    // below can check none of them leak into the response.
    private const string UnreachableDatabase =
        "Host=127.0.0.1;Port=1;Database=leak_check_db;Username=leak_check_user;Password=leak_check_pw;Timeout=3";

    [Fact]
    public async Task Health_is_200_Healthy_when_the_database_is_reachable() // B7
    {
        using var client = fixture.Factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Health_is_503_Unhealthy_when_the_database_is_unreachable() // B8, B9
    {
        // Its own factory, pointing at the dead address. `await using`
        // disposes it (and the in-memory app) at the end of the test.
        await using var factory = new ApiFactory(UnreachableDatabase);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        // B8: 503 Service Unavailable, the standard "I'm up, but I can't do
        // my job" status. Load balancers and monitors understand it.
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        // B9: the body is exactly one word. Assert.Equal on the whole body
        // is the strongest form of "no details": nothing else can be in it.
        Assert.Equal("Unhealthy", body);

        // B9, said explicitly, so a future custom response writer can't
        // quietly start leaking: none of the connection details appear.
        Assert.DoesNotContain("127.0.0.1", body);
        Assert.DoesNotContain("leak_check", body);
    }

    [Fact]
    public async Task The_api_keeps_answering_when_the_database_is_down() // B8
    {
        await using var factory = new ApiFactory(UnreachableDatabase);
        using var client = factory.CreateClient();

        // A failing health check must not take the API down with it.
        using var health = await client.GetAsync("/health");
        using var root = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, root.StatusCode);
    }
}

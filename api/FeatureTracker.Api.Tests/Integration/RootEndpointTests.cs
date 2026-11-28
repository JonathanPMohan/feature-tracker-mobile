using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FeatureTracker.Api.Tests.Integration;

// The simplest integration test: the pattern every later endpoint test
// follows. Read this one first.
//
// [Collection(...)] joins the shared-Postgres group (PostgresFixture.cs).
// xUnit sees the PostgresFixture constructor parameter and passes in the one
// shared instance: dependency injection, for tests.
[Collection(PostgresCollection.Name)]
public sealed class RootEndpointTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Get_root_returns_the_api_name_as_json()
    {
        // Arrange: an HttpClient wired straight into the in-memory API.
        using var client = fixture.Factory.CreateClient();

        // Act: a real GET request through routing, the endpoint and JSON
        // serialization. "/" is relative: the client already knows the app.
        using var response = await client.GetAsync("/");

        // Assert: the status first. If this fails, the JSON checks below
        // would only add noise.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        // Read the body as generic JSON rather than as ApiInfo: ApiInfo is
        // `internal` to the API project, and checking the raw JSON also
        // proves the property is camelCase ("name"), which is what the
        // phone will receive.
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Feature Tracker API", json.GetProperty("name").GetString());
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Aihrly.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Aihrly.Tests.Integration;

public class ApplicationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private const string AliceId = "11111111-1111-1111-1111-111111111111";

    public ApplicationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    }

    private async Task<Guid> CreateJobAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/jobs", new
        {
            title = "Test Job",
            description = "Test Description",
            location = "Remote"
        });

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return Guid.Parse(json.RootElement.GetProperty("id").GetString()!);
    }

    private async Task<Guid> CreateApplicationAsync(Guid jobId, string email)
    {
        var response = await _client.PostAsJsonAsync($"/api/jobs/{jobId}/applications", new
        {
            candidateName = "Test Candidate",
            candidateEmail = email
        });

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return Guid.Parse(json.RootElement.GetProperty("id").GetString()!);
    }

    [Fact]
    public async Task AddNote_ThenGetNotes_ReturnsAuthorName()
    {
        var jobId = await CreateJobAsync();
        var applicationId = await CreateApplicationAsync(jobId, $"note-{Guid.NewGuid()}@test.com");

        _client.DefaultRequestHeaders.Remove("X-Team-Member-Id");
        _client.DefaultRequestHeaders.Add("X-Team-Member-Id", AliceId);

        var noteResponse = await _client.PostAsJsonAsync($"/api/applications/{applicationId}/notes", new
        {
            type = "General",
            description = "Great candidate"
        });

        Assert.Equal(HttpStatusCode.Created, noteResponse.StatusCode);

        var notesResponse = await _client.GetAsync($"/api/applications/{applicationId}/notes");
        Assert.Equal(HttpStatusCode.OK, notesResponse.StatusCode);

        var json = JsonDocument.Parse(await notesResponse.Content.ReadAsStringAsync());
        var notes = json.RootElement.EnumerateArray().ToList();

        Assert.Single(notes);
        Assert.Equal("Alice Johnson", notes[0].GetProperty("authorName").GetString());
    }

    [Fact]
    public async Task SubmitScoreTwice_SecondValueWins()
    {
        var jobId = await CreateJobAsync();
        var applicationId = await CreateApplicationAsync(jobId, $"score-{Guid.NewGuid()}@test.com");

        _client.DefaultRequestHeaders.Remove("X-Team-Member-Id");
        _client.DefaultRequestHeaders.Add("X-Team-Member-Id", AliceId);

        await _client.PutAsJsonAsync($"/api/applications/{applicationId}/scores/culture-fit", new
        {
            score = 3,
            comment = "First submission"
        });

        await _client.PutAsJsonAsync($"/api/applications/{applicationId}/scores/culture-fit", new
        {
            score = 5,
            comment = "Second submission"
        });

        var profileResponse = await _client.GetAsync($"/api/applications/{applicationId}");
        var json = JsonDocument.Parse(await profileResponse.Content.ReadAsStringAsync());

        var scores = json.RootElement.GetProperty("scores").EnumerateArray().ToList();
        var cultureFit = scores.First(s => s.GetProperty("dimension").GetString() == "CultureFit");

        Assert.Equal(5, cultureFit.GetProperty("score").GetInt32());
        Assert.Equal("Second submission", cultureFit.GetProperty("comment").GetString());
        Assert.NotEqual(JsonValueKind.Null, cultureFit.GetProperty("updatedAt").ValueKind);
    }

    [Fact]
    public async Task DuplicateApplication_SameEmailSameJob_Returns409()
    {
        var jobId = await CreateJobAsync();
        var email = $"dup-{Guid.NewGuid()}@test.com";

        var first = await _client.PostAsJsonAsync($"/api/jobs/{jobId}/applications", new
        {
            candidateName = "John Doe",
            candidateEmail = email
        });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync($"/api/jobs/{jobId}/applications", new
        {
            candidateName = "John Doe",
            candidateEmail = email
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }
}

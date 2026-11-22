using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Features.Subjects.SuggestSubjects;
using TutorMatch.Api.Infrastructure.Persistence;
using Xunit;

namespace TutorMatch.Tests.Features.Subjects.SuggestSubjects;

public class SubjectSuggestionEndpointTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SubjectSuggestionEndpointTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TutorMatchDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<TutorMatchDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                });
            });
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        db.Database.EnsureCreated();

        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Given_ValidQuery_When_CallingSuggestEndpoint_Then_ReturnsOkWithSuggestions()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            db.Subjects.AddRange(
                new Subject("Algoritmi", usageCount: 15),
                new Subject("Algoritmi Avansati", usageCount: 20),
                new Subject("Arhitectura Calculatoarelor", usageCount: 5)
            );
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/subjects/suggest?q=alg");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<SubjectSuggestionResponse>();
        content.Should().NotBeNull();
        content!.Suggestions.Should().HaveCount(2);
        content.Suggestions[0].Should().Be("Algoritmi Avansati");
        content.Suggestions[1].Should().Be("Algoritmi");
    }

    [Fact]
    public async Task Given_EmptyQuery_When_CallingSuggestEndpoint_Then_ReturnsOkWithEmptyList()
    {
        var response = await _client.GetAsync("/api/subjects/suggest?q=");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<SubjectSuggestionResponse>();
        content.Should().NotBeNull();
        content!.Suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_V2ApiRoute_When_CallingSuggestEndpoint_Then_ReturnsOk()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            db.Subjects.Add(new Subject("Database Systems", usageCount: 7));
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/v2/subjects/suggest?q=data");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<SubjectSuggestionResponse>();
        content.Should().NotBeNull();
        content!.Suggestions.Should().Contain("Database Systems");
    }
}

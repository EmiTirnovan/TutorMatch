using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Features.Discovery.SearchTutors;
using TutorMatch.Api.Infrastructure.Persistence;
using Xunit;

namespace TutorMatch.Tests.Features.Discovery.SearchTutors;

public class SearchTutorsEndpointTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly SqliteConnection _connection;
    private readonly HttpClient _client;

    public SearchTutorsEndpointTests()
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

    private async Task SeedDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();

        // Curățăm datele vechi (în caz că există ceva global)
        db.Tutors.RemoveRange(db.Tutors);
        db.Set<Subject>().RemoveRange(db.Set<Subject>());
        await db.SaveChangesAsync();

        var mathSubject = new Subject("Matematica");
        var infoSubject = new Subject("Informatica");
        db.Set<Subject>().AddRange(mathSubject, infoSubject);

        var tutor1 = new Tutor("Tutor Mate Ieftin", "mate@test.com", "Bio mate", 50m);
        tutor1.AddSubject(mathSubject);
        tutor1.AddAvailability(new DateRange(DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2)));

        var tutor2 = new Tutor("Tutor Info Scump", "info@test.com", "Bio info", 150m);
        tutor2.AddSubject(infoSubject);
        tutor2.AddAvailability(new DateRange(DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(2).AddHours(2)));

        db.Tutors.AddRange(tutor1, tutor2);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Given_SearchRequestWithSubject_When_Searching_Then_ReturnsFilteredTutors()
    {
        // Arrange
        await SeedDataAsync();
        var requestUrl = "/api/tutors/search?subject=Matematica";

        // Act
        var response = await _client.GetAsync(requestUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<SearchTutorsResponse>();
        
        content.Should().NotBeNull();
        content!.Mode.Should().Be("keyword");
        content.Tutors.Should().HaveCount(1);
        content.Tutors.First().Name.Should().Be("Tutor Mate Ieftin");
    }

    [Fact]
    public async Task Given_SearchRequestWithPriceRange_When_Searching_Then_ReturnsFilteredTutors()
    {
        // Arrange
        await SeedDataAsync();
        var requestUrl = "/api/tutors/search?minPrice=100&maxPrice=200";

        // Act
        var response = await _client.GetAsync(requestUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<SearchTutorsResponse>();
        
        content.Should().NotBeNull();
        content!.Tutors.Should().HaveCount(1);
        content.Tutors.First().Name.Should().Be("Tutor Info Scump");
    }

    [Fact]
    public async Task Given_SearchRequest_When_Searching_Then_ResponseExposesSubjectsOfEachTutor()
    {
        // Arrange
        await SeedDataAsync();

        // Act
        var response = await _client.GetAsync("/api/tutors/search?sortBy=priceAsc");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<SearchTutorsResponse>();

        content.Should().NotBeNull();
        var tutors = content!.Tutors.ToList();
        tutors.Select(t => t.Name).Should().Equal("Tutor Mate Ieftin", "Tutor Info Scump");
        tutors[0].Subjects.Should().Equal("Matematica");
        tutors[1].Subjects.Should().Equal("Informatica");
    }

    [Fact]
    public async Task Given_SearchRequest_When_Searching_Then_JsonUsesCamelCaseSubjectsArray()
    {
        // Arrange
        await SeedDataAsync();

        // Act
        var json = await _client.GetStringAsync("/api/tutors/search?subject=Matematica");

        // Assert
        json.Should().Contain("\"subjects\":[\"Matematica\"]");
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
        _factory.Dispose();
        _client.Dispose();
    }
}
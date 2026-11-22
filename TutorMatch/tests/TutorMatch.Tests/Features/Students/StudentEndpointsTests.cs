using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Domain.Students;
using TutorMatch.Api.Features.Students.GetStudentById;
using TutorMatch.Api.Features.Students.UpdateProfile;
using TutorMatch.Api.Infrastructure.Persistence;
using Xunit;

namespace TutorMatch.Tests.Features.Students;

public class StudentEndpointsTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public StudentEndpointsTests()
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
    public async Task Given_ExistingStudent_When_GettingById_Then_ReturnsOkAndDetails()
    {
        var studentId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            db.Students.Add(new Student(studentId, "Diana Pop", "diana@student.upt.ro"));
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/students/{studentId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<GetStudentByIdResponse>();
        content.Should().NotBeNull();
        content!.Id.Should().Be(studentId);
        content.Name.Should().Be("Diana Pop");
        content.Email.Should().Be("diana@student.upt.ro");
    }

    [Fact]
    public async Task Given_ExistingStudent_When_UpdatingProfile_Then_ReturnsNoContentAndPersists()
    {
        var studentId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            db.Students.Add(new Student(studentId, "Initial Diana", "diana@student.upt.ro"));
            await db.SaveChangesAsync();
        }

        var updateRequest = new UpdateStudentProfileRequest("Diana Pop Updated");
        var response = await _client.PutAsJsonAsync($"/api/students/{studentId}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var updated = await db.Students.FindAsync(studentId);
            updated.Should().NotBeNull();
            updated!.Name.Should().Be("Diana Pop Updated");
        }
    }

    [Fact]
    public async Task Given_InvalidData_When_UpdatingStudentProfile_Then_ReturnsBadRequest()
    {
        var studentId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            db.Students.Add(new Student(studentId, "Diana", "diana@student.upt.ro"));
            await db.SaveChangesAsync();
        }

        var updateRequest = new UpdateStudentProfileRequest("   ");
        var response = await _client.PutAsJsonAsync($"/api/students/{studentId}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

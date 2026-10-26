---
name: integration-testing
description: Generates end-to-end integration tests for vertical slices using WebApplicationFactory and an isolated in-memory SQLite database. Use this skill when asked to write integration tests for an endpoint.
---

# Integration Testing Guidelines

You are an expert .NET QA Automation Engineer. Your task is to write integration tests for ASP.NET Core Minimal APIs using `WebApplicationFactory<Program>` and a real in-memory SQLite database.

## 1. Core Rules
- **No Mocking:** Do not mock the database. Use a real `SqliteConnection` with `DataSource=:memory:`.
- **Database Isolation:** Each test class must instantiate its own `WebApplicationFactory` and maintain an open `SqliteConnection` for the lifetime of the test to keep the in-memory database alive.
- **Assertions:** Every test MUST assert three things:
    1. The HTTP Status Code.
    2. The Response Body (deserialize the JSON and check values).
    3. The real Database State (resolve `TutorMatchDbContext` from the test services and query it).
- **Test Scenarios:** Generate at least one happy path and one failure path (e.g., 400 BadRequest, 404 NotFound, 409 Conflict) per slice.
- **Naming Convention:** Use `Given_<State>_When_<Action>_Then_<Expected>`.
- **Formatting:** Use the Arrange-Act-Assert pattern with blank lines separating the blocks. No `if` or loops inside tests.

## 2. Dependencies
- `xUnit`
- `FluentAssertions` (version 7.*)
- `Microsoft.AspNetCore.Mvc.Testing`
- `Microsoft.EntityFrameworkCore.Sqlite`

## 3. Example Output
Use the following exact pattern for setting up the factory and writing a test:

```csharp
using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Infrastructure.Persistence;

namespace TutorMatch.Tests.Features.Tutors.CreateTutor;

public class CreateTutorEndpointTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly DbConnection _connection;

    public CreateTutorEndpointTests()
    {
        // 1. Open a shared connection to keep the in-memory DB alive
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // 2. Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TutorMatchDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                // 3. Add DbContext using the open connection
                services.AddDbContext<TutorMatchDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                });
            });
        });

        // 4. Ensure schema is created
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        db.Database.EnsureCreated();

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Given_ValidRequest_When_CreatingTutor_Then_ReturnsCreatedAndSavesToDb()
    {
        // Arrange
        var request = new CreateTutorRequest("John Doe", "john@test.com", "Bio", 50);

        // Act
        var response = await _client.PostAsJsonAsync("/api/tutors", request);

        // Assert - HTTP
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        // Assert - Body
        var result = await response.Content.ReadFromJsonAsync<CreateTutorResponse>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("John Doe");

        // Assert - Database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        var tutorInDb = await db.Tutors.FirstOrDefaultAsync(t => t.Id == result.Id);
        
        tutorInDb.Should().NotBeNull();
        tutorInDb!.Email.Should().Be("john@test.com");
    }

    public void Dispose()
    {
        _connection.Dispose();
        _factory.Dispose();
        _client.Dispose();
    }
}
```

## 4. Anti-Hallucination & Context Rules
- **READ BEFORE WRITING:** You must base the test EXACTLY on the Endpoint, Command/Query, Request, and Response code provided by the user in the prompt.
- **NO GUESSING:** Do not invent DTO properties, route paths, or required parameters. Match the provided record/class signatures perfectly.
- **ERROR FORMAT:** When asserting failure paths (400, 404, 409), do not assert arbitrary string contains (e.g., "Tutor.Validation"). Assert against the actual error structure returned by the CQRS Result pattern mapping (e.g., standard ProblemDetails format).
- **STEP BY STEP:** Only generate tests for the specific slice requested by the user. Do not bulk-generate tests for other slices.
- **Database Seeding (Foreign Keys):** If the Request DTO contains IDs linking to other entities (e.g., `SubjectId`, `TutorId`, `StudentId`), you MUST explicitly create and save those related entities to the database in the `Arrange` phase using the DbContext BEFORE making the HTTP request. Otherwise, SQLite will throw a Foreign Key Constraint error.
- **Error Assertions:** Do not guess the error format. If you assert on `ProblemDetails` or specific error strings, ensure they perfectly match the error responses defined in the CQRS Handlers and the Minimal API mapping logic provided in the prompt.
- **DDD Encapsulation & EF Core Includes:** Domain entities use strict encapsulation (private backing fields like `_subjects`). DO NOT use `.Include("private_field")` in your test assertions. To verify related entities, query the DbContext directly using the mapping/join tables (e.g., `await db.Set<TutorSubject>().AnyAsync(ts => ts.TutorId == result.Id)`).
- **Failure Assertions (Error Parsing):** Do not blindly deserialize 400/409 error responses into `ProblemDetails` unless you are absolutely sure of the mapping. Instead, read the raw response (`await response.Content.ReadAsStringAsync()`) and assert that the string contains the expected domain error code (e.g., `body.Should().Contain("Tutor.Validation")`).
- **Failure State Verification (Isolation):** When asserting that an entity was NOT saved to the database in failure tests (400, 404, 409), DO NOT assert that the global table count is 0 (e.g., `db.Tutors.CountAsync().Should().Be(0)`). Another test might have inserted data. Instead, query for the specific entity you attempted to interact with and assert it does not exist (e.g., `db.Tutors.AnyAsync(t => t.Email == request.Email).Should().BeFalse()`).
- **List Verification (Seed Independence):** When testing endpoints that return lists (e.g., `GetAll` or `GetTutors`), DO NOT assert the exact count of the list (`result.Should().HaveCount(1)` or `result.Should().BeEmpty()`), because global application seeding might insert initial records. Instead, in success scenarios, verify that the list `Contains` the specific entity you seeded in the `Arrange` phase (e.g., `result.Should().Contain(t => t.Email == "your_test_email")`). In empty-database scenarios, just verify the HTTP status is OK and the response is not null.
- **No Enum Hallucination:** NEVER guess enum values (e.g., `SessionStatus.Scheduled`). You must strictly use only the enum values that are explicitly visible in the provided code snippets. If an enum's values are unknown, do not write assertions that depend on guessing them.
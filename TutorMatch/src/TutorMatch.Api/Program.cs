using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Adaugam documentatia Swagger si infrastructura
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSessionInfrastructure();
builder.Services.AddTutorInfrastructure();

var app = builder.Build();

// Swagger activat pentru testare interactiva in browser
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "TutorMatch API is running.");

// Endpoint-uri pentru testarea manuala a repository-ului de tutori
var tutorsApi = app.MapGroup("/api/tutors").WithTags("Tutors");

tutorsApi.MapGet("/", (ITutorRepository repository) =>
{
    return Results.Ok(repository.GetAll());
});

tutorsApi.MapGet("/{id:guid}", (Guid id, ITutorRepository repository) =>
{
    var tutor = repository.GetById(id);
    return tutor is not null ? Results.Ok(tutor) : Results.NotFound();
});

tutorsApi.MapPost("/", (CreateTutorRequest request, ITutorRepository repository) =>
{
    try
    {
        var tutor = new Tutor(request.Name, request.Email, request.Bio, request.HourlyRate);
        repository.Add(tutor);
        return Results.Created($"/api/tutors/{tutor.Id}", tutor);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
});

// Seed cu un tutore demo ca sa avem date vizibile imediat la testare
var tutorRepo = app.Services.GetRequiredService<ITutorRepository>();
if (tutorRepo.GetAll().Count == 0)
{
    var demoTutor = new Tutor("Emi Tirnovan", "emi@tutormatch.ro", "Student anul 3. Ajut cu Algoritmi si POO.", 75m);
    var tomorrow = DateTime.UtcNow.Date.AddDays(1);
    demoTutor.AddAvailability(new DateRange(tomorrow.AddHours(10), tomorrow.AddHours(14)));
    tutorRepo.Add(demoTutor);
}

app.Run();

// DTO simplu pentru crearea unui tutor prin API
public record CreateTutorRequest(string Name, string Email, string Bio, decimal HourlyRate);

public partial class Program
{
}

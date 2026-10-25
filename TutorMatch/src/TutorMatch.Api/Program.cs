using Microsoft.EntityFrameworkCore;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Features;
using TutorMatch.Api.Features.Sessions;
using TutorMatch.Api.Features.Tutors;
using TutorMatch.Api.Infrastructure;
using TutorMatch.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure SQLite persistence replacing in-memory repositories
builder.Services.AddSqliteRepositories(
    builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=tutormatch.db");

builder.Services.AddCqrs();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "TutorMatch API is running.");

app.MapTutorEndpoints();
app.MapSessionEndpoints();

// Migrate SQLite database and seed initial demo tutor inside a scoped container
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
    dbContext.Database.Migrate();

    var tutorRepo = scope.ServiceProvider.GetRequiredService<ITutorRepository>();
    if (tutorRepo.GetAll().Count == 0)
    {
        var demoTutor = new Tutor("Emi Tirnovan", "emi@tutormatch.ro", "Student anul 3. Ajut cu Algoritmi si POO.", 75m);
        var tomorrow = DateTime.UtcNow.Date.AddDays(1);
        demoTutor.AddAvailability(new DateRange(tomorrow.AddHours(10), tomorrow.AddHours(14)));
        tutorRepo.Add(demoTutor);
    }
}

app.Run();

public partial class Program
{
}

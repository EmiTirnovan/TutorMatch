using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Features.Tutors;
using TutorMatch.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSessionInfrastructure();
builder.Services.AddTutorInfrastructure();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "TutorMatch API is running.");

app.MapTutorEndpoints();

var tutorRepo = app.Services.GetRequiredService<ITutorRepository>();
if (tutorRepo.GetAll().Count == 0)
{
    var demoTutor = new Tutor("Emi Tirnovan", "emi@tutormatch.ro", "Student anul 3. Ajut cu Algoritmi si POO.", 75m);
    var tomorrow = DateTime.UtcNow.Date.AddDays(1);
    demoTutor.AddAvailability(new DateRange(tomorrow.AddHours(10), tomorrow.AddHours(14)));
    tutorRepo.Add(demoTutor);
}

app.Run();

public partial class Program
{
}

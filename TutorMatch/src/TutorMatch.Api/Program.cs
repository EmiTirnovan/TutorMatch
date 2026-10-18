using TutorMatch.Api.Features.Tutors;
using TutorMatch.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Register Infrastructure services
builder.Services.AddSessionInfrastructure();

var app = builder.Build();

app.MapGet("/", () => "TutorMatch API is running.");

app.MapTutorEndpoints();

app.Run();

// Make Program accessible to integration tests if needed
public partial class Program
{
}

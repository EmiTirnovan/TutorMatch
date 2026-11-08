using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Students;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Features;
using TutorMatch.Api.Features.SemanticSearch.Notifications;
using TutorMatch.Api.Features.Sessions;
using TutorMatch.Api.Features.Tutors;
using TutorMatch.Api.Infrastructure;
using TutorMatch.Api.Infrastructure.Persistence;
using TutorMatch.Api.Features.Discovery;

// Bootstrapping logic cannot be effectively unit-tested.
[ExcludeFromCodeCoverage]
public class Program
{
    public static void Main(String[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v2", new OpenApiInfo
            {
                Title = "TutorMatch API v2",
                Version = "v2",
                Description = "TutorMatch API Version 2 - Next generation endpoints with subject selection and custom details."
            });
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "TutorMatch API v1",
                Version = "v1",
                Description = "TutorMatch API Version 1"
            });
        });

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            });
        });

        // Configure SQLite persistence replacing in-memory repositories
        builder.Services.AddSqliteRepositories(
            builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=tutormatch.db");

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddCqrs();

        var app = builder.Build();

        app.UseCors();

        app.Use(async (context, next) =>
        {
            var feature = context.Features.Get<IHttpResponseBodyFeature>();
            if (feature != null)
            {
                var needsWrap = false;
                try
                {
                    _ = feature.Writer.UnflushedBytes;
                }
                catch (Exception)
                {
                    needsWrap = true;
                }

                if (needsWrap)
                {
                    context.Features.Set<IHttpResponseBodyFeature>(new CompatibilityResponseBodyFeature(feature));
                }
            }

            await next();
        });

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v2/swagger.json", "TutorMatch API v2 (Current)");
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "TutorMatch API v1");
            });
        }

        app.MapGet("/", () => "TutorMatch API is running.");

        app.MapTutorEndpoints();
        app.MapSessionEndpoints();
        app.MapDiscoveryEndpoints();    

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

            // shortcut: no auth yet, the frontend books as this student (DEMO_STUDENT_ID); replace when identity lands.
            var demoStudentId = Guid.Parse("23a81600-7740-49c2-9de8-ad86816f5291");
            var studentRepo = scope.ServiceProvider.GetRequiredService<IStudentRepository>();
            if (studentRepo.GetById(demoStudentId) is null)
            {
                studentRepo.Add(new Student(demoStudentId, "Alex Radu", "alex.radu@student.upt.ro"));
            }
        }

        // Register in-process subscriber for AI embedding generation
        TutorSavedNotificationHandler.RegisterSubscriber(app.Services.GetRequiredService<IServiceScopeFactory>());

        app.Run();

    }
}

[ExcludeFromCodeCoverage]
internal sealed class CompatibilityResponseBodyFeature : IHttpResponseBodyFeature
{
    private readonly IHttpResponseBodyFeature _inner;
    private readonly CompatibilityPipeWriter _writer;

    public CompatibilityResponseBodyFeature(IHttpResponseBodyFeature inner)
    {
        _inner = inner;
        _writer = new CompatibilityPipeWriter(inner.Writer);
    }

    public Stream Stream => _inner.Stream;
    public PipeWriter Writer => _writer;

    public Task CompleteAsync() => _inner.CompleteAsync();
    public void DisableBuffering() => _inner.DisableBuffering();
    public Task SendFileAsync(String path, Int64 offset, Int64? count, CancellationToken cancellationToken = default) =>
        _inner.SendFileAsync(path, offset, count, cancellationToken);
    public Task StartAsync(CancellationToken cancellationToken = default) => _inner.StartAsync(cancellationToken);
}

[ExcludeFromCodeCoverage]
internal sealed class CompatibilityPipeWriter : PipeWriter
{
    private readonly PipeWriter _inner;

    public CompatibilityPipeWriter(PipeWriter inner)
    {
        _inner = inner;
    }

    public override bool CanGetUnflushedBytes => true;

    public override Int64 UnflushedBytes
    {
        get
        {
            try
            {
                return _inner.UnflushedBytes;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }

    public override void Advance(Int32 bytes) => _inner.Advance(bytes);
    public override void CancelPendingFlush() => _inner.CancelPendingFlush();
    public override void Complete(Exception? exception = null) => _inner.Complete(exception);
    public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default) =>
        _inner.FlushAsync(cancellationToken);
    public override Memory<Byte> GetMemory(Int32 sizeHint = 0) => _inner.GetMemory(sizeHint);
    public override Span<Byte> GetSpan(Int32 sizeHint = 0) => _inner.GetSpan(sizeHint);
}

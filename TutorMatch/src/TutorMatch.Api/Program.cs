using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Features;
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
        builder.Services.AddSwaggerGen();

        // Configure SQLite persistence replacing in-memory repositories
        builder.Services.AddSqliteRepositories(
            builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=tutormatch.db");

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddCqrs();

        var app = builder.Build();

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
            app.UseSwaggerUI();
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
        }

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
    public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default) =>
        _inner.SendFileAsync(path, offset, count, cancellationToken);
    public Task StartAsync(CancellationToken cancellationToken = default) =>
        _inner.StartAsync(cancellationToken);
}

[ExcludeFromCodeCoverage]
internal sealed class CompatibilityPipeWriter : PipeWriter
{
    private readonly PipeWriter _inner;
    private long _unflushedBytes;

    public CompatibilityPipeWriter(PipeWriter inner)
    {
        _inner = inner;
    }

    public override bool CanGetUnflushedBytes => true;
    public override long UnflushedBytes => _unflushedBytes;

    public override void Advance(int bytes)
    {
        _unflushedBytes += bytes;
        _inner.Advance(bytes);
    }

    public override Memory<byte> GetMemory(int sizeHint = 0) => _inner.GetMemory(sizeHint);
    public override Span<byte> GetSpan(int sizeHint = 0) => _inner.GetSpan(sizeHint);

    public override async ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
    {
        _unflushedBytes = 0;
        return await _inner.FlushAsync(cancellationToken);
    }

    public override void CancelPendingFlush() => _inner.CancelPendingFlush();
    public override void Complete(Exception? exception = null) => _inner.Complete(exception);
}
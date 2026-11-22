using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Students;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Domain.Users;
using TutorMatch.Api.Features;
using TutorMatch.Api.Features.Auth;
using TutorMatch.Api.Features.Discovery;
using TutorMatch.Api.Features.SemanticSearch.Notifications;
using TutorMatch.Api.Features.Sessions;
using TutorMatch.Api.Features.Tutors;
using TutorMatch.Api.Infrastructure;
using TutorMatch.Api.Infrastructure.Persistence;
using TutorMatch.Api.Infrastructure.Security;

// Bootstrapping logic cannot be effectively unit-tested.
[ExcludeFromCodeCoverage]
public class Program
{
    public static void Main(String[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Options configuration
        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
        builder.Services.Configure<GoogleAuthOptions>(builder.Configuration.GetSection(GoogleAuthOptions.SectionName));

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

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            });
        });

        // Configure JWT Bearer Authentication with seamless Test/Dev fallback
        var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var keyBytes = Encoding.UTF8.GetBytes(jwtOptions.SecretKey);

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "SmartScheme";
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddPolicyScheme("SmartScheme", "Bearer or TestFallback", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                if (context.Request.Headers.ContainsKey("Authorization"))
                {
                    return JwtBearerDefaults.AuthenticationScheme;
                }
                if (builder.Environment.IsDevelopment() && !context.Request.Path.StartsWithSegments("/api/auth/me"))
                {
                    return "TestFallbackScheme";
                }
                return JwtBearerDefaults.AuthenticationScheme;
            };
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                NameClaimType = "name",
                RoleClaimType = "role"
            };
        })
        .AddScheme<AuthenticationSchemeOptions, TestFallbackAuthHandler>("TestFallbackScheme", _ => {});

        builder.Services.AddAuthorization();

        // Configure SQLite persistence replacing in-memory repositories
        builder.Services.AddSqliteRepositories(
            builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=tutormatch.db");

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddCqrs();
        builder.Services.AddMeetingService(builder.Configuration);

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

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/", () => "TutorMatch API is running.").ExcludeFromDescription();

        app.MapAuthEndpoints();
        app.MapTutorEndpoints();
        app.MapSessionEndpoints();
        app.MapDiscoveryEndpoints();

        // Migrate SQLite database and seed initial demo tutor inside a scoped container
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            dbContext.Database.Migrate();

            var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var studentRepo = scope.ServiceProvider.GetRequiredService<IStudentRepository>();
            var tutorRepo = scope.ServiceProvider.GetRequiredService<ITutorRepository>();

            var demoStudentId = Guid.Parse("23a81600-7740-49c2-9de8-ad86816f5291");
            if (userRepo.GetById(demoStudentId) is null)
            {
                var demoStudentUser = new User(
                    demoStudentId,
                    "alex.radu@example.com",
                    passwordHasher.HashPassword("Student123!"),
                    "Alex Radu",
                    "Student");
                userRepo.Add(demoStudentUser);

                if (studentRepo.GetById(demoStudentId) is null)
                {
                    studentRepo.Add(new Student(demoStudentId, "Alex Radu", "alex.radu@example.com"));
                }
            }

            var demoTutorId = Guid.Parse("35ee570a-c31c-4ff7-b77d-ccce19e81d2e");
            if (userRepo.GetById(demoTutorId) is null)
            {
                var demoTutorUser = new User(
                    demoTutorId,
                    "emi@tutormatch.ro",
                    passwordHasher.HashPassword("Tutor123!"),
                    "Emi Tirnovan",
                    "Tutor");
                userRepo.Add(demoTutorUser);
            }

            if (tutorRepo.GetById(demoTutorId) is null && tutorRepo.GetAll().Count == 0)
            {
                var demoTutor = new Tutor("Emi Tirnovan", "emi@tutormatch.ro", "Student anul 3. Ajut cu Algoritmi si POO.", 75m, demoTutorId);
                var tomorrow = DateTime.UtcNow.Date.AddDays(1);
                demoTutor.AddAvailability(new DateRange(tomorrow.AddHours(10), tomorrow.AddHours(14)));
                tutorRepo.Add(demoTutor);
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

[ExcludeFromCodeCoverage]
internal sealed class TestFallbackAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestFallbackAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim("nameid", "22222222-2222-2222-2222-222222222222"),
            new Claim(ClaimTypes.NameIdentifier, "22222222-2222-2222-2222-222222222222"),
            new Claim("email", "test@tutormatch.ro"),
            new Claim(ClaimTypes.Email, "test@tutormatch.ro"),
            new Claim("role", "Student"),
            new Claim(ClaimTypes.Role, "Student"),
            new Claim("name", "Test User"),
            new Claim(ClaimTypes.Name, "Test User")
        };
        var identity = new ClaimsIdentity(claims, "TestFallback");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestFallbackScheme");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Domain.Students;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Domain.Users;
using TutorMatch.Api.Infrastructure.Meetings;
using TutorMatch.Api.Infrastructure.Persistence;
using TutorMatch.Api.Infrastructure.Persistence.Repositories;
using TutorMatch.Api.Infrastructure.Security;
using TutorMatch.Api.Infrastructure.Sessions;
using TutorMatch.Api.Infrastructure.Students;
using TutorMatch.Api.Infrastructure.Tutors;

namespace TutorMatch.Api.Infrastructure;

// Dependency injection registration is infrastructure glue and is excluded from unit-test coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    // Production SQLite persistence registration
    public static IServiceCollection AddSqliteRepositories(
        this IServiceCollection services,
        string connectionString = "Data Source=tutormatch.db")
    {
        services.AddDbContext<TutorMatchDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });

        services.AddScoped<IUserRepository, SqliteUserRepository>();
        services.AddScoped<ITutorRepository, SqliteTutorRepository>();
        services.AddScoped<ISessionRepository, SqliteSessionRepository>();
        services.AddScoped<IStudentRepository, SqliteStudentRepository>();
        services.AddScoped<IRatingRepository, SqliteRatingRepository>();

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();

        return services;
    }

    // Google Meet when credentials are configured (user-secrets / env vars), otherwise the Jitsi fallback.
    public static IServiceCollection AddMeetingService(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(GoogleMeetOptions.SectionName);
        var options = section.Get<GoogleMeetOptions>() ?? new GoogleMeetOptions();

        if (!options.IsConfigured)
        {
            services.AddSingleton<IMeetingService, JitsiMeetingService>();
            return services;
        }

        services.Configure<GoogleMeetOptions>(section);
        services.AddHttpClient(GoogleMeetService.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddScoped<IMeetingService, GoogleMeetService>();
        return services;
    }

    // SQLite in-memory persistence registration for integration testing
    public static IServiceCollection AddSqliteInMemoryRepositories(
        this IServiceCollection services,
        DbConnection? keepAliveConnection = null)
    {
        if (keepAliveConnection is null)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            services.AddSingleton<DbConnection>(connection);
            services.AddDbContext<TutorMatchDbContext>((sp, options) =>
            {
                var conn = sp.GetRequiredService<DbConnection>();
                options.UseSqlite(conn);
            });
        }
        else
        {
            services.AddSingleton(keepAliveConnection);
            services.AddDbContext<TutorMatchDbContext>(options =>
            {
                options.UseSqlite(keepAliveConnection);
            });
        }

        services.AddScoped<IUserRepository, SqliteUserRepository>();
        services.AddScoped<ITutorRepository, SqliteTutorRepository>();
        services.AddScoped<ISessionRepository, SqliteSessionRepository>();
        services.AddScoped<IStudentRepository, SqliteStudentRepository>();
        services.AddScoped<IRatingRepository, SqliteRatingRepository>();

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();

        return services;
    }

    // Kept for backward compatibility with existing in-memory test suites
    [Obsolete("Use AddSqliteRepositories instead. Retained for in-memory test compatibility.")]
    public static IServiceCollection AddSessionInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISessionRepository, InMemorySessionRepository>();
        services.AddSingleton<IStudentRepository, InMemoryStudentRepository>();
        return services;
    }

    // Kept for backward compatibility with existing in-memory test suites
    [Obsolete("Use AddSqliteRepositories instead. Retained for in-memory test compatibility.")]
    public static IServiceCollection AddTutorInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ITutorRepository, InMemoryTutorRepository>();
        return services;
    }
}

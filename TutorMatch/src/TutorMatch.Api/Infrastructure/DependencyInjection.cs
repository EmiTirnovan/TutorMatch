using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Infrastructure.Persistence;
using TutorMatch.Api.Infrastructure.Persistence.Repositories;
using TutorMatch.Api.Infrastructure.Sessions;
using TutorMatch.Api.Infrastructure.Tutors;

namespace TutorMatch.Api.Infrastructure;

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

        services.AddScoped<ITutorRepository, SqliteTutorRepository>();
        services.AddScoped<ISessionRepository, SqliteSessionRepository>();

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

        services.AddScoped<ITutorRepository, SqliteTutorRepository>();
        services.AddScoped<ISessionRepository, SqliteSessionRepository>();

        return services;
    }

    // Kept for backward compatibility with existing in-memory test suites
    [Obsolete("Use AddSqliteRepositories instead. Retained for in-memory test compatibility.")]
    public static IServiceCollection AddSessionInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISessionRepository, InMemorySessionRepository>();
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
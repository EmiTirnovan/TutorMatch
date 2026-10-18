using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Infrastructure.Sessions;
using TutorMatch.Api.Infrastructure.Tutors;

namespace TutorMatch.Api.Infrastructure;

// Inregistrarea serviciilor in containerul de Dependency Injection
public static class DependencyInjection
{
    public static IServiceCollection AddSessionInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISessionRepository, InMemorySessionRepository>();
        return services;
    }

    public static IServiceCollection AddTutorInfrastructure(this IServiceCollection services)
    {
        // Inregistram repository-ul de tutori ca Singleton in memorie
        services.AddSingleton<ITutorRepository, InMemoryTutorRepository>();
        return services;
    }
}
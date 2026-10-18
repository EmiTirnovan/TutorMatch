using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Infrastructure.Sessions;

namespace TutorMatch.Api.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSessionInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISessionRepository, InMemorySessionRepository>();
        return services;
    }
}
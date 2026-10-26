using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Features.Sessions.CreateSession;
using TutorMatch.Api.Features.Sessions.DeleteSession;
using TutorMatch.Api.Features.Sessions.GetSessionById;
using TutorMatch.Api.Features.Sessions.GetSessions;
using TutorMatch.Api.Features.Sessions.UpdateSession;
using TutorMatch.Api.Features.Tutors.CreateTutor;
using TutorMatch.Api.Features.Tutors.DeleteTutor;
using TutorMatch.Api.Features.Tutors.GetTutorById;
using TutorMatch.Api.Features.Tutors.GetTutors;
using TutorMatch.Api.Features.Tutors.UpdateTutor;

namespace TutorMatch.Api.Features;

// Bootstrapping registration code is excluded from unit testing.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddCqrs(this IServiceCollection services)
    {
        // Tutors CQRS Handlers
        services.AddScoped<ICommandHandler<CreateTutorCommand, Result<CreateTutorResponse>>, CreateTutorCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateTutorCommand>, UpdateTutorCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteTutorCommand>, DeleteTutorCommandHandler>();
        services.AddScoped<IQueryHandler<GetTutorByIdQuery, Result<GetTutorByIdResponse>>, GetTutorByIdQueryHandler>();
        services.AddScoped<IQueryHandler<GetTutorsQuery, Result<List<TutorSummaryResponse>>>, GetTutorsQueryHandler>();

        // Sessions CQRS Handlers
        services.AddScoped<ICommandHandler<CreateSessionCommand, Result<CreateSessionResponse>>, CreateSessionCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateSessionCommand>, UpdateSessionCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteSessionCommand>, DeleteSessionCommandHandler>();
        services.AddScoped<IQueryHandler<GetSessionByIdQuery, Result<GetSessionByIdResponse>>, GetSessionByIdQueryHandler>();
        services.AddScoped<IQueryHandler<GetSessionsQuery, Result<List<GetSessionsResponse>>>, GetSessionsQueryHandler>();

        return services;
    }
}

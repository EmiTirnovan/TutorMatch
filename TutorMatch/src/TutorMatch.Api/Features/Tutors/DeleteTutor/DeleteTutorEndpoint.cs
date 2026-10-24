using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Tutors.DeleteTutor;

public static class DeleteTutorEndpoint
{
    public static IEndpointRouteBuilder MapDeleteTutor(this IEndpointRouteBuilder group)
    {
        group.MapDelete("/{id:guid}", HandleAsync);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ICommandHandler<DeleteTutorCommand> handler,
        CancellationToken cancellationToken)
    {
        var command = new DeleteTutorCommand(id);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToHttpResult();
    }
}

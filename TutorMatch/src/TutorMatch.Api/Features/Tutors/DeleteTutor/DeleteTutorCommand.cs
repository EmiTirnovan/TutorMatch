using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Tutors.DeleteTutor;

public record DeleteTutorCommand(Guid Id) : ICommand;

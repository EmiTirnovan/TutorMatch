---
name: cqrs
description: Transforms API vertical slices into CQRS Commands and Queries with hand-written handlers behind small interfaces, DI registration, and functional Result types for expected failures without external mediator libraries.
---

# CQRS (Commands and Queries) Skill

This skill guides the architecture and code generation for converting vertical slice endpoints into CQRS with hand-written handlers, explicit interfaces, dependency injection, and functional result handling.

## Core Rules

1. **Commands vs. Queries**:
   - `Create`, `Update`, `Delete` actions MUST be implemented as **Commands**.
   - `GetById`, `GetAll`, and search actions MUST be implemented as **Queries**.
   - **Queries MUST NEVER change system state.** They are strictly read-only.

2. **No External Mediator**:
   - Do NOT use MediatR or any third-party mediator library.
   - Handlers must implement small, explicit interfaces:
     - `ICommandHandler<TCommand, TResult>`
     - `ICommandHandler<TCommand>` (returns `Task<Result>`)
     - `IQueryHandler<TQuery, TResult>`
   - Handlers are resolved directly via ASP.NET Core Dependency Injection.

3. **Error Handling via Result Types**:
   - Handlers MUST return a `Result` or `Result<T>` type for all expected business and validation outcomes.
   - Handlers MUST NEVER throw exceptions for expected failures (e.g. Not Found, Validation, Conflict / Concurrency).
   - Expected error categories:
     - `ErrorType.NotFound` -> Maps to HTTP 404
     - `ErrorType.Validation` / BadRequest -> Maps to HTTP 400
     - `ErrorType.Conflict` -> Maps to HTTP 409
   - Domain model validation exceptions should be caught inside the handler and translated into `Result.Failure(Error.Validation(...))`.

4. **Thin Endpoints**:
   - Endpoints are minimal route handlers whose only responsibility is:
     1. Accept the HTTP request / route params.
     2. Map to a Command or Query.
     3. Invoke the corresponding Handler via DI.
     4. Map the resulting `Result` to the appropriate HTTP status code (`TypedResults.Ok`, `TypedResults.Created`, `TypedResults.NoContent`, `TypedResults.NotFound`, `TypedResults.BadRequest`, `TypedResults.Conflict`).

5. **Folder & File Layout**:
   - Every use case lives in its own dedicated vertical slice folder:
     `Features/<Aggregate>/<UseCase>/`
   - Files within each use case:
     - `<UseCase>Command.cs` or `<UseCase>Query.cs`
     - `<UseCase>CommandHandler.cs` or `<UseCase>QueryHandler.cs`
     - `<UseCase>Request.cs` (if request body exists and differs from command)
     - `<UseCase>Response.cs` (if a response payload is returned)
     - `<UseCase>Endpoint.cs` (maps endpoint to route group)

## Interfaces & Result Structure

```csharp
// Common/Results/Result.cs
public record Error(string Code, string Description, ErrorType Type)
{
    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);
    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);
    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);
}

public enum ErrorType
{
    Validation = 400,
    NotFound = 404,
    Conflict = 409
}

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }
    // ... Success() / Failure(Error error)
}

public class Result<T> : Result
{
    public T Value { get; }
    // ... Success(T value) / Failure(Error error)
}
```

```csharp
// Common/Cqrs/ICommandHandler.cs
public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

public interface ICommandHandler<in TCommand>
{
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

// Common/Cqrs/IQueryHandler.cs
public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
```

## Example: Command Slice

### 1. Command
```csharp
namespace TutorMatch.Api.Features.Tutors.CreateTutor;

public record CreateTutorCommand(
    string Name,
    string Email,
    string Bio,
    decimal HourlyRate,
    List<Guid>? SubjectIds = null,
    List<DateRangeDto>? Availability = null
);
```

### 2. Handler
```csharp
namespace TutorMatch.Api.Features.Tutors.CreateTutor;

public class CreateTutorCommandHandler : ICommandHandler<CreateTutorCommand, Result<CreateTutorResponse>>
{
    private readonly ITutorRepository _repository;

    public CreateTutorCommandHandler(ITutorRepository repository)
    {
        _repository = repository;
    }

    public Task<Result<CreateTutorResponse>> HandleAsync(CreateTutorCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var tutor = new Tutor(command.Name, command.Email, command.Bio, command.HourlyRate);

            if (command.SubjectIds != null)
            {
                foreach (var subjectId in command.SubjectIds)
                {
                    tutor.AddSubject(subjectId);
                }
            }

            if (command.Availability != null)
            {
                foreach (var slot in command.Availability)
                {
                    tutor.AddAvailability(new DateRange(slot.Start, slot.End));
                }
            }

            _repository.Add(tutor);

            var response = new CreateTutorResponse(tutor.Id, tutor.Name, tutor.Email, tutor.Bio, tutor.HourlyRate);
            return Task.FromResult(Result<CreateTutorResponse>.Success(response));
        }
        catch (ArgumentException ex)
        {
            return Task.FromResult(Result<CreateTutorResponse>.Failure(Error.Validation("Tutor.Validation", ex.Message)));
        }
        catch (InvalidOperationException ex)
        {
            return Task.FromResult(Result<CreateTutorResponse>.Failure(Error.Conflict("Tutor.Conflict", ex.Message)));
        }
    }
}
```

### 3. Endpoint
```csharp
namespace TutorMatch.Api.Features.Tutors.CreateTutor;

public static class CreateTutorEndpoint
{
    public static IEndpointRouteBuilder MapCreateTutor(this IEndpointRouteBuilder group)
    {
        group.MapPost("/", HandleAsync);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        CreateTutorRequest request,
        ICommandHandler<CreateTutorCommand, Result<CreateTutorResponse>> handler,
        CancellationToken ct)
    {
        var command = new CreateTutorCommand(
            request.Name,
            request.Email,
            request.Bio,
            request.HourlyRate,
            request.SubjectIds,
            request.Availability);

        var result = await handler.HandleAsync(command, ct);

        return result.Match(
            onSuccess: response => TypedResults.Created($"/api/tutors/{response.Id}", response),
            onFailure: error => error.Type switch
            {
                ErrorType.NotFound => TypedResults.NotFound(error.Description),
                ErrorType.Conflict => TypedResults.Conflict(error.Description),
                _ => TypedResults.BadRequest(error.Description)
            });
    }
}
```

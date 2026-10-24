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

2. **No External Mediator & Marker Interfaces**:
   - Do NOT use MediatR or any third-party mediator library.
   - Commands and Queries implement marker interfaces:
     - `ICommand<TResponse>` for commands that return a value.
     - `ICommand` for void commands (returning only `Task<Result>`).
     - `IQuery<TResponse>` for queries.
   - Handlers implement explicit, focused interfaces with constraints:
     - `ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>`
     - `ICommandHandler<in TCommand> where TCommand : ICommand` (returns `Task<Result>`)
     - `IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>`
   - Handlers are resolved directly via ASP.NET Core Dependency Injection.
   - Always accept `CancellationToken cancellationToken = default` in handlers and pass it down to any asynchronous I/O operations.

3. **Error Handling via Result Types**:
   - Handlers MUST return a `Result` or `Result<T>` type for all expected business and validation outcomes.
   - Handlers MUST NEVER throw exceptions for expected failures (e.g. Not Found, Validation, Conflict / Concurrency).
   - Expected error categories:
     - `ErrorType.NotFound` -> Maps to HTTP 404
     - `ErrorType.Validation` -> Maps to HTTP 400
     - `ErrorType.Conflict` -> Maps to HTTP 409
     - `ErrorType.Failure` -> Maps to HTTP 500
   - Domain model validation exceptions should be caught inside the handler and translated into `Result.Failure(Error.Validation(...))` or `Result.Failure(Error.Conflict(...))`.
   - Errors are returned as structured objects `{ Code, Description }`.

4. **Thin Endpoints & ResultExtensions**:
   - Endpoints are minimal route handlers whose only responsibility is:
     1. Accept the HTTP request / route params.
     2. Map to a Command or Query.
     3. Invoke the corresponding Handler via DI.
     4. Map the resulting `Result` to HTTP responses using the unified extension method:
        - `result.ToHttpResult()` -> `204 NoContent` (for void Commands) or `200 OK` (for Queries / Value Commands).
        - `result.ToHttpResult(res => TypedResults.Created(...))` -> `201 Created` (for Create Commands).
        - Maps failures automatically to `400 BadRequest`, `404 NotFound`, or `409 Conflict`.

5. **Folder & File Layout**:
   - Every use case lives in its own dedicated vertical slice folder:
     `Features/<Aggregate>/<UseCase>/`
   - Files within each use case:
     - `<UseCase>Command.cs` or `<UseCase>Query.cs`
     - `<UseCase>CommandHandler.cs` or `<UseCase>QueryHandler.cs`
     - `<UseCase>Request.cs` (if request body exists and differs from command)
     - `<UseCase>Response.cs` (if a response payload is returned)
     - `<UseCase>Endpoint.cs` (maps endpoint to route group)
   - Dependency Injection registration file:
     `Features/DependencyInjection.cs` with `AddCqrs(this IServiceCollection services)`.

---

## Interfaces & Result Structure

```csharp
// Common/Results/Error.cs
public record Error(string Code, string Description, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);
    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);
    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);
    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);
}

public enum ErrorType
{
    Validation = 400,
    NotFound = 404,
    Conflict = 409,
    Failure = 500
}

// Common/Results/Result.cs
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
}

public class Result<TValue> : Result
{
    public TValue Value { get; }
    public static Result<TValue> Success(TValue value) => new(value, true, Error.None);
    public static new Result<TValue> Failure(Error error) => new(default, false, error);
}

// Common/Results/ResultExtensions.cs
public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess ? TypedResults.NoContent() : MapError(result.Error);

    public static IResult ToHttpResult<TValue>(this Result<TValue> result, Func<TValue, IResult>? onSuccess = null) =>
        result.IsSuccess
            ? (onSuccess != null ? onSuccess(result.Value) : TypedResults.Ok(result.Value))
            : MapError(result.Error);

    private static IResult MapError(Error error) => error.Type switch
    {
        ErrorType.NotFound => TypedResults.NotFound(new { error.Code, error.Description }),
        ErrorType.Conflict => TypedResults.Conflict(new { error.Code, error.Description }),
        _ => TypedResults.BadRequest(new { error.Code, error.Description })
    };
}
```

```csharp
// Common/Cqrs/ICommandHandler.cs
public interface ICommand;
public interface ICommand<TResponse>;

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

// Common/Cqrs/IQueryHandler.cs
public interface IQuery<TResponse>;

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
```

---

## Concrete Slice Examples

### 1. Command with Result (Create)
```csharp
// Command
namespace TutorMatch.Api.Features.Tutors.CreateTutor;

public record CreateTutorCommand(
    string Name,
    string Email,
    string Bio,
    decimal HourlyRate,
    List<Guid>? SubjectIds = null,
    List<DateRangeDto>? Availability = null
) : ICommand<Result<CreateTutorResponse>>;

// Handler
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

            if (command.SubjectIds is not null)
            {
                foreach (var subjectId in command.SubjectIds)
                {
                    tutor.AddSubject(subjectId);
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

// Endpoint
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
            request.Name, request.Email, request.Bio, request.HourlyRate, request.SubjectIds, request.Availability);

        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult(response => TypedResults.Created($"/api/tutors/{response.Id}", response));
    }
}
```

### 2. Command without Payload (Update / Delete)
```csharp
// Command
namespace TutorMatch.Api.Features.Tutors.DeleteTutor;

public record DeleteTutorCommand(Guid Id) : ICommand;

// Handler
public class DeleteTutorCommandHandler : ICommandHandler<DeleteTutorCommand>
{
    private readonly ITutorRepository _repository;

    public DeleteTutorCommandHandler(ITutorRepository repository)
    {
        _repository = repository;
    }

    public Task<Result> HandleAsync(DeleteTutorCommand command, CancellationToken cancellationToken = default)
    {
        var deleted = _repository.Delete(command.Id);
        if (!deleted)
        {
            return Task.FromResult(Result.Failure(
                Error.NotFound("Tutor.NotFound", $"Tutor with ID '{command.Id}' was not found.")));
        }

        return Task.FromResult(Result.Success());
    }
}

// Endpoint
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
        CancellationToken ct)
    {
        var command = new DeleteTutorCommand(id);
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult();
    }
}
```

### 3. Query (Get By Id)
```csharp
// Query
namespace TutorMatch.Api.Features.Tutors.GetTutorById;

public record GetTutorByIdQuery(Guid Id) : IQuery<Result<GetTutorByIdResponse>>;

// Handler
public class GetTutorByIdQueryHandler : IQueryHandler<GetTutorByIdQuery, Result<GetTutorByIdResponse>>
{
    private readonly ITutorRepository _repository;

    public GetTutorByIdQueryHandler(ITutorRepository repository)
    {
        _repository = repository;
    }

    public Task<Result<GetTutorByIdResponse>> HandleAsync(GetTutorByIdQuery query, CancellationToken cancellationToken = default)
    {
        var tutor = _repository.GetById(query.Id);
        if (tutor is null)
        {
            return Task.FromResult(Result<GetTutorByIdResponse>.Failure(
                Error.NotFound("Tutor.NotFound", $"Tutor with ID '{query.Id}' was not found.")));
        }

        var response = new GetTutorByIdResponse(tutor.Id, tutor.Name, tutor.Email, tutor.Bio, tutor.HourlyRate, ...);
        return Task.FromResult(Result<GetTutorByIdResponse>.Success(response));
    }
}

// Endpoint
public static class GetTutorByIdEndpoint
{
    public static IEndpointRouteBuilder MapGetTutorById(this IEndpointRouteBuilder group)
    {
        group.MapGet("/{id:guid}", HandleAsync);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        IQueryHandler<GetTutorByIdQuery, Result<GetTutorByIdResponse>> handler,
        CancellationToken ct)
    {
        var query = new GetTutorByIdQuery(id);
        var result = await handler.HandleAsync(query, ct);
        return result.ToHttpResult();
    }
}
```

### 4. Dependency Injection Registration (`Features/DependencyInjection.cs`)
```csharp
namespace TutorMatch.Api.Features;

public static class DependencyInjection
{
    public static IServiceCollection AddCqrs(this IServiceCollection services)
    {
        // Commands
        services.AddScoped<ICommandHandler<CreateTutorCommand, Result<CreateTutorResponse>>, CreateTutorCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteTutorCommand>, DeleteTutorCommandHandler>();
        // Queries
        services.AddScoped<IQueryHandler<GetTutorByIdQuery, Result<GetTutorByIdResponse>>, GetTutorByIdQueryHandler>();
        
        return services;
    }
}
```

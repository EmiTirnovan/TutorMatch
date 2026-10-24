---
name: code-review
description: Inspects and performs static code analysis on TutorMatch repository according to vertical slice architecture, CQRS principles, domain invariants, test quality, naming standards, and functional error handling. Strictly read-only outputting an issue summary and a structured Markdown table.
---

# Code Review & Static Analysis Skill

This skill governs the automated and systematic static review of C# / .NET codebases adhering to Vertical Slice Architecture, CQRS without third-party mediators, domain-driven invariants, and functional error handling with `Result` types.

---

## 1. Operating Rules & Constraints

1. **Strictly Read-Only Behavior**:
   - The skill **ONLY** inspects, reads, and analyzes files across the repository.
   - It is **STRICTLY FORBIDDEN** to modify, create, delete, or stage any source code files (`.cs`, `.csproj`, etc.) or tests while executing this review.
   - All proposed code changes must be expressed purely as text descriptions in the output table under the `Proposed fix` column.

2. **Mandatory Anti-Hallucination Policy**:
   - Only report concrete, verifiable defects and discrepancies that physically exist in the analyzed codebase.
   - If a category or the entire codebase has no actual defects, explicitly report `0 issues found` and do NOT invent, speculate, or inflate non-existent issues.
   - Every cited `File:Line` must point to a valid relative path from the repository root and to a real existing line number in the source file.

---

## 2. Output Format Specification

The output of this skill must strictly follow this exact two-part format:

### Part 1: Initial Summary Line
A single line stating the total number of issues found, broken down by severity:
```text
Summary: X issues found (Y Critical, Z Major, W Minor)
```
*Clean run example:*
```text
Summary: 0 issues found (0 Critical, 0 Major, 0 Minor)
```

### Part 2: Markdown Table
A single Markdown table with exactly the following columns:
```markdown
| # | File:Line | Severity | Category | Problem | Proposed fix |
|---|-----------|----------|----------|---------|--------------|
| 1 | path/to/File.cs:42 | Critical | domain invariants | Detailed problem description | Proposed concrete fix |
```

---

## 3. Classification and Sorting Rules

### Allowed Severities (Strictly 3 levels)
Every reported issue must be assigned exactly one of these three severities:

1. **`Critical`**:
   - Broken domain invariants leading to corrupted state, invalid domain objects, or concurrency collisions (e.g. overlapping sessions, missing version increments on state change, exceeding daily session limits).
   - Unhandled exceptions inside command/query handlers that bypass the `Result` pattern and crash requests with 500 errors.
   - Queries that mutate persistent state or commands that bypass data consistency boundaries.
   - Security vulnerabilities, data leaks, or severe logic bugs.

2. **`Major`**:
   - Leaky vertical slice boundaries: direct dependencies or references between distinct feature slices (e.g. slice A importing requests/DTOs from slice B).
   - CQRS deviations: handlers failing to implement `ICommandHandler` / `IQueryHandler`, missing `CancellationToken` propagation, or endpoints not using `.ToHttpResult()`.
   - Missing or incorrect error mapping (`ErrorType` not matching the business failure: e.g. using `Validation` instead of `Conflict` or `NotFound`).
   - Critical absence of tests for entire vertical slices, leaving core handlers and workflows unverified.

3. **`Minor`**:
   - Naming convention discrepancies: mismatch between file name and declared class/record, inconsistent slice type names (e.g. not following `<UseCase>Command`, `<UseCase>Endpoint`, `<UseCase>Response`), or test method names not following `UnitOfWork_StateUnderTest_ExpectedBehavior`.
   - Redundant `using` directives, minor dead code, missing parameter null checks in secondary methods, or slight inconsistencies in code styling/formatting.

### Sorting Order
The rows in the table **MUST** be sorted descending by severity:
1. All `Critical` issues first.
2. All `Major` issues second.
3. All `Minor` issues third.

---

## 4. Fixed Categories (Strictly 6 categories)

Every reported issue must belong to exactly one of the following 6 fixed categories, defined in the context of the `TutorMatch` architecture and `.claude/skills/cqrs/SKILL.md`:

### 1. `slice boundaries`
- **Scope**: Vertical slice isolation in `Features/<Aggregate>/<UseCase>/`.
- **Checks**:
  - Each vertical slice must be self-contained and encapsulated.
  - Slices must **never** import or reuse request DTOs, response models, or commands from another slice (e.g., `GetTutorById` importing a DTO declared in `CreateTutor/CreateTutorRequest.cs`).
  - Shared models must reside strictly in `Domain` (for entities and value objects) or `Common` (for cross-cutting DTOs/contracts).

### 2. `CQRS violations`
- **Scope**: Separation of Commands and Queries according to `.claude/skills/cqrs/SKILL.md`.
- **Checks**:
  - Commands mutate state (`Create`, `Update`, `Delete`) and implement `ICommand` or `ICommand<TResponse>`.
  - Queries are strictly read-only (`GetById`, `GetAll`, `Search`) and implement `IQuery<TResponse>`. Queries must **never** alter state.
  - Handlers implement explicit interfaces (`ICommandHandler<TCommand, TResponse>`, `ICommandHandler<TCommand>`, `IQueryHandler<TQuery, TResponse>`).
  - Handlers accept `CancellationToken cancellationToken = default` and pass it to async calls.
  - No external mediator libraries (e.g., MediatR) — direct ASP.NET Core DI resolution.
  - Endpoints remain thin route delegates mapping to handlers and calling `result.ToHttpResult(...)`.

### 3. `domain invariants`
- **Scope**: Business logic and state consistency enforced by aggregates and value objects in `Domain/`.
- **Checks**:
  - Value object validity (e.g., `DateRange`: `Start < End`, non-negative durations, `Overlaps`, `Contains`).
  - Invariant R1: Tutor sessions never overlap (`DateRange.Overlaps`).
  - Invariant R2: Optimistic concurrency control via `Version` increment on updates.
  - Invariant R3: Cancellation rule (free cancellation ≥ 12h before start; otherwise `LateCancelled`).
  - Invariant R4: Maximum 2 sessions per student per day.
  - Invariant R5: Ratings allowed only after a session is `Completed`, and only once per session.
  - Aggregates encapsulate private collections (`_availability`, `_tutorSubjects`, `_sessions`) exposing only `IReadOnlyList<T>`.

### 4. `test quality`
- **Scope**: Unit, integration, and architectural tests in `TutorMatch.Tests`.
- **Checks**:
  - Meaningful assertions using xUnit and FluentAssertions; no empty tests or test methods without assertions.
  - Test naming follows `UnitOfWork_StateUnderTest_ExpectedBehavior` (e.g., `BookSession_WhenTutorHasOverlap_ShouldThrowInvalidOperationException`).
  - Proper coverage of edge cases: boundary dates, zero/negative rates, null parameters, concurrent requests.
  - No empty test fixtures or placeholder directories without test implementations.

### 5. `naming`
- **Scope**: Architectural, C#, and vertical slice naming conventions.
- **Checks**:
  - Vertical slices: `<UseCase>Command`, `<UseCase>CommandHandler`, `<UseCase>Endpoint`, `<UseCase>Request`, `<UseCase>Response`, `<UseCase>Query`, `<UseCase>QueryHandler`.
  - Type name matches the enclosing file name (e.g., file `GetTutorsResponse.cs` must declare `GetTutorsResponse`, not `TutorSummaryResponse`).
  - Domain models: Singular nouns for entities (`Tutor`, `Session`, `Student`), descriptive value objects (`DateRange`).
  - Interfaces prefixed with `I` (`ITutorRepository`, `ISessionRepository`, `ICommandHandler`).

### 6. `error handling`
- **Scope**: Functional error handling via `Result` and `Result<T>`.
- **Checks**:
  - Handlers must never throw uncaught exceptions for expected business or validation failures.
  - Domain exceptions (`ArgumentException`, `InvalidOperationException`) caught in handlers and converted into `Result.Failure(Error.Validation(...))` or `Result.Failure(Error.Conflict(...))`.
  - Proper `ErrorType` classification:
    - `Validation` (400) for malformed inputs or domain constraint violations.
    - `NotFound` (404) for non-existent entities.
    - `Conflict` (409) for concurrent modifications or schedule overlaps.
    - `Failure` (500) for unexpected system failures.
  - Endpoints utilize `result.ToHttpResult(...)` for clean HTTP response generation.


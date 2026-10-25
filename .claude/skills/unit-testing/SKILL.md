---
name: unit-testing
description: Writes xUnit unit tests (FluentAssertions 7.x, hand-written fakes, no mocking libraries) for Domain/ classes and CQRS command/query handlers in this .NET project, following the repo's AAA and naming conventions and targeting 100% coverage. Use when the user asks to write, add, extend or fix unit tests, improve code coverage, or test a domain model or handler. Not for endpoints or integration tests, and never modifies production code.
---

# Unit Testing Skill

You are an expert C# .NET developer generating unit tests for Domain models and CQRS Handlers. Your objective is to achieve 100% code coverage while strictly enforcing the quality conventions and AAA patterns outlined below. Do not generate production code with this skill, only test code.

## Dependencies & Licensing

- Use **xUnit** as the primary testing framework.
- Use **FluentAssertions** for all assertions. You must strictly use version `7.*` to comply with the Apache 2.0 license.

## Naming Conventions & Structure

- **Class Naming:** Name the test class exactly `<ClassUnderTest>Tests`.
- **Method Naming:** Use the strict format `Given_<state>_When_<action>_Then_<expected>`.
  - _Example:_ `Given_ExpiredReservation_When_Confirming_Then_ReturnsConflict`.
- **AAA Pattern:** You must structure every test method with clear `Arrange`, `Act`, and `Assert` sections. Separate each section with exactly one blank line. Do not write comments for these sections.
- **Single Behavior:** Test exactly one behavior per method.
- **No Control Logic:** You are strictly forbidden from using control flow statements (`if`, `for`, `foreach`, `while`, `switch`) inside test methods.
- **Data-Driven Tests:** Use `[Theory]` combined with `[InlineData]` or `[MemberData]` when testing multiple input variants for the same behavior.
- **Assertion Strategy:** Assert on behavior and outcomes, never on internal implementation details.

## Isolation & Fakes (Strictly Enforced)

- **No Mocking Libraries:** You must not use any mocking libraries (e.g., Moq, NSubstitute, FakeItEasy). All dependencies must be isolated using manually written Fakes (e.g., in-memory dictionary repositories, stub classes).
- **Time Independence:** For any temporal business rules, inject and use a fake clock (e.g., `Microsoft.Extensions.Time.Testing.FakeTimeProvider` or a custom `IClock` fake). Never use `DateTime.Now` or `DateTime.UtcNow` directly.
- **Execution Order:** Tests must be completely independent and not rely on execution order or shared mutable state.

## Folder & File Structure (Strict 1:1 Mapping)

- **Strict 1:1 File Mapping:** You MUST create exactly one test file for every production file being tested. **NEVER compress, combine, or group tests for multiple handlers or slices into a single generic file** (e.g., do NOT create a generic `SessionHandlerTests.cs` for all session commands).
- **Exact Folder Mirroring:** The directory structure of the test project must faithfully mirror the directory structure of the production code.
- Example (workspace-anchored, no ambiguity):
  - Workspace root is the folder that contains README.md and TutorMatch/.
  - In this repository, source handlers are under TutorMatch/src/TutorMatch.Api/Features/... and tests are under TutorMatch/tests/TutorMatch.Tests/Features/...
  - Source: TutorMatch/src/TutorMatch.Api/Features/Sessions/CreateSession/CreateSessionCommandHandler.cs
    Test: TutorMatch/tests/TutorMatch.Tests/Features/Sessions/CreateSession/CreateSessionCommandHandlerTests.cs
  - Source: TutorMatch/src/TutorMatch.Api/Features/Sessions/DeleteSession/DeleteSessionCommandHandler.cs
    Test: TutorMatch/tests/TutorMatch.Tests/Features/Sessions/DeleteSession/DeleteSessionCommandHandlerTests.cs
  - Never create files outside the current workspace root. If a path cannot be resolved inside the workspace, stop and ask for clarification.

## Autonomous Execution Strategy (Strictly Enforced)

When asked to apply this skill to the workspace, you must act autonomously and sequentially without stopping, strictly following these steps:

**Step 1: The Exclusion Sweep (Infrastructure & DTOs)**

- Actively search the workspace for all '*Endpoint.cs', '*Request.cs', '*Response.cs', '*Command.cs', '*Query.cs', and '*DependencyInjection.cs' files, plus Program.cs and everything inside Infrastructure/ and Persistence/.
- Modify EVERY single one of these files to include `[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]` above the class declaration.
- The mandatory 1-line justification comment for Commands, Queries, and Requests MUST be: `// DTOs and messages contain no business logic and are excluded from unit testing.`

**Step 2: The Core Generation (Domain, Handlers & Architectural Primitives)**

- Actively search the workspace for all classes in the `Domain/` folder, all CQRS `Handler` classes, AND any custom architectural primitives they inherently depend on (specifically the custom `Result` and `Error` pattern types returned by the handlers).
- Because these Result types encapsulate core application behavior and contain purely unit-testable logic, they MUST be included in the 100% coverage target. NEVER exclude them.
- Generate exhaustive tests for every single logical path (if/else, throw, validations), including sequential state mutations and object mappings, ensuring true 100% line coverage.

**Step 3: Self-Verification & Correction (The 100% Loop)**

- After generating the tests, you MUST run this exact command in the terminal to verify your own work: `dotnet test --collect:"XPlat Code Coverage"` followed by `reportgenerator -reports:"**/TestResults/**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html`
- DO NOT assume 100% coverage just because the terminal exit code is 0 (tests passed). You MUST physically parse the generated coverage report.
- **Line-Coverage Blindspot:** If Branch Coverage is 100% but Line Coverage is lower, you missed sequential operations (property assignments, mappings). Pinpoint the exact uncovered lines in the report and add the missing test scenarios.
- Keep looping and fixing in small batches until the report mathematically confirms 100%.

## Existing Tests & Legacy Code (Strictly Enforced)

- **Evaluate Before Modifying:** If tests already exist for the target Domain or Handler, evaluate them against this skill's rules first.
- **Conditional Refactoring:** If existing tests violate ANY rule (e.g., using standard asserts, lacking AAA structure, wrong naming, or using mocking libraries), you MUST completely refactor them to comply.
- **Preserve Compliant Code:** If existing tests already fully comply with these rules, DO NOT rewrite them. Only add new tests if coverage is missing.

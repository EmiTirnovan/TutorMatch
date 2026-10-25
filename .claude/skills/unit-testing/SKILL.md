---
name: unit-testing
description: Generates unit tests for domain models and CQRS handlers
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

## Folder Structure

- The directory structure of the test project must faithfully mirror the directory structure of the production code (e.g., tests for `Features/Sessions/CreateSession/CreateSessionCommandHandler.cs` must reside in `Features/Sessions/CreateSession/CreateSessionCommandHandlerTests.cs`).

## Coverage & Reporting

- **Goal:** 100% code coverage on all `Domain/` classes and all CQRS Handlers.
- **Endpoints Exception:** Endpoints are exclusively covered by integration tests; do not generate unit tests for endpoints.
- **Tooling:** Tests must be compatible with `coverlet.collector` for coverage measurement and `ReportGenerator` for HTML reporting.
- **Exclusions:** Exclude only what fundamentally cannot be unit-tested (e.g., `Program.cs`, generated code, or external wrappers). Apply the `[ExcludeFromCodeCoverage]` attribute and immediately precede it with a single-line comment explicitly justifying the exclusion.

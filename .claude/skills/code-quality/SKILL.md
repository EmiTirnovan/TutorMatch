---
name: code-quality
description: Comprehensive standards and guidelines for code quality, architectural consistency, and maintainability across C# / .NET backend slices and Angular frontend features. Use when designing, writing, reviewing, or refactoring code in TutorMatch.
---

# TutorMatch Code Quality & Engineering Standards

This skill defines the technical, architectural, and design system quality standards for **TutorMatch**. All code written or reviewed across the full stack—C# / .NET backend and TypeScript / Angular frontend—must comply with these guidelines.

---

## 1. General Principles

1. **Simplicity Over Cleverness (KISS & YAGNI)**:
   - Prefer explicit, readable code over abstract meta-programming or premature optimization.
   - Do not add abstractions (e.g. MediatR, generic unit-of-work wrappers, complex state stores) until multiple concrete use cases demand them.

2. **Self-Documenting Code**:
   - Write expressive identifiers for variables, functions, and types that clearly explain intent.
   - Keep comments concise, contextual, and in English. Avoid redundant comments that simply repeat the code.

3. **No Unhandled Failures**:
   - Avoid throwing exceptions for business or validation failures. Use the `Result` / `Result<T>` pattern with well-defined `Error` objects.
   - Ensure the user interface always receives structured, human-understandable error responses (`ProblemDetails` or specific error DTOs).

---

## 2. Backend Standards (.NET / C#)

### 2.1 Vertical Slice Architecture & CQRS
- **Feature Folders**: Each use case lives in its own dedicated feature folder (`Features/{Module}/{FeatureName}/`) containing:
  - `{Feature}Command.cs` / `{Feature}Query.cs`
  - `{Feature}CommandHandler.cs` / `{Feature}QueryHandler.cs`
  - `{Feature}Request.cs` (if HTTP DTO differs from command)
  - `{Feature}Response.cs`
  - `{Feature}Endpoint.cs` (Minimal API route definition)
- **Slice Boundaries**: Never share feature-specific DTOs between slices. Common value objects or system-wide DTOs belong strictly in `Common/Dtos/` or `Domain/{Module}/`.
- **Async & Cancellation**: Propagate `CancellationToken` through all asynchronous handler and repository calls.

### 2.2 Domain-Driven Design (DDD)
- **Rich Aggregates & Invariants**: Enforce business rules inside the aggregate root (`Tutor`, `Session`). Never expose mutable public setters on domain properties.
- **Value Objects**: Encapsulate concepts with structural equality and self-validation (e.g., `DateRange` validating `Start < End` and providing `Overlaps()` and `Contains()`).
- **Domain Events & Transactional Safety**:
  - Domain events (e.g. `TutorSavedNotification`) must only be published **after** the database transaction successfully commits.
  - Never allow asynchronous or uncommitted operations to trigger external side-effects (such as AI vector embeddings) before data is persisted.

### 2.3 Persistence (EF Core & SQLite)
- Isolate queries using explicit projection (`Select`) or split queries (`AsSplitQuery()`) where entity graphs are loaded.
- Use explicit database configurations (`IEntityTypeConfiguration<T>`) rather than polluting domain classes with EF attributes.

---

## 3. Frontend Standards (Angular & TypeScript)

### 3.1 Modern Angular Architecture
- **Standalone Components**: Use `standalone: true` for all components, directives, and pipes. Do not use legacy `NgModule`.
- **Reactivity with Signals**:
  - Prefer Angular Signals (`signal()`, `computed()`, `effect()`) for component local state.
  - Use `toSignal()` or `async` pipe when consuming RxJS Observables from `HttpClient`.
- **Strict Typing**:
  - Never use `any`. Always declare explicit TypeScript interfaces or types for API requests, responses, and view models.
  - Enable `strict` mode in `tsconfig.json`.

### 3.2 Forms & Validation
- Use **Reactive Forms** (`FormGroup`, `FormControl`, `FormBuilder`) with typed controls.
- Implement synchronous and asynchronous validators cleanly. Provide immediate visual feedback for validation errors (e.g. start date $\ge$ end date, overlapping intervals).

### 3.3 Design System Compliance (`design.md`)
- **Palette**:
  - Primary: Midnight Navy (`#0F172A`, `#1E293B`)
  - Accent: Violet / Blue-Violet (`#6366F1`, `#4F46E5`) for primary actions and AI highlights
  - Background: Cool Gray (`#F8FAFC`, `#F1F5F9`) with crisp white surfaces (`#FFFFFF`)
  - Status: Restrained Green (Success), Amber (Warning / Late cancellation), Red (Errors)
- **Typography & Layout**:
  - Clean sans-serif typography (`Inter`, `system-ui`).
  - Sentence case for titles, headings, and buttons.
  - Clean consumer SaaS appearance: generous spacing, subtle borders (`#E2E8F0`), restrained shadows. Avoid exaggerated "AI neon" gradients or noisy glassmorphism.
- **Resilient States**:
  - Always design and render: Default, Loading (skeletons), Empty, and Error states.
  - Handle HTTP `409 Conflict` gracefully with informative dialogs.

---

## 4. Testing Standards

### 4.1 Unit Testing (C# & TypeScript)
- **Naming Convention**: `Given_<State>_When_<Action>_Then_<Expected>` (e.g. `Given_ValidInterval_When_AddingAvailability_Then_Succeeds`).
- **No Mocking Libraries for Domain Logic**: Use fakes or in-memory implementations. Domain aggregates and value objects must be tested purely in-memory.
- **Fluent Assertions**: Use FluentAssertions (7.*) in C# and Jasmine/Jest matchers in TypeScript for readable assertions.
- **Deterministic Time**: Use `TimeProvider` or injectable clock abstractions when testing time-sensitive rules (e.g. 12-hour cancellation threshold).

### 4.2 Integration & Component Testing
- Verify end-to-end HTTP endpoints with `WebApplicationFactory` and in-memory SQLite.
- Verify Angular component DOM rendering, user interactions, and event emissions.

---

## 5. Definition of Quality Checklist (DoQ)

Before submitting or reviewing code, ensure:
- [ ] No compilation warnings or unresolved lint errors.
- [ ] Domain invariants are enforced inside entities/aggregates, not leaked into controllers or UI.
- [ ] Events dispatch safely only after database commit.
- [ ] UI states adhere to the `design.md` color tokens and layout guidelines.
- [ ] Unit and integration tests pass deterministically (100% green).

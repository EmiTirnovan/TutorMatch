# TutorMatch – Claude Skills & Prompts Log (PROMPTS.md)

Acest document înregistrează toate interacțiunile, prompt-urile executate, skill-urile rulate, problemele identificate în codul generat și ajustările aduse fișierelor `SKILL.md` pentru obținerea unui cod conform specificațiilor.

---

## 1. Skill `cqrs`: Commands and Queries

### Prompt inițial
> "Implementează arhitectura CQRS pentru toate vertical slices din Features/Tutors și Features/Sessions folosind skill-ul cqrs. Creează Commands pentru Create, Update, Delete și Queries pentru GetById, GetAll. Folosește hand-written handlers, interfețe DI specifice și Result type pentru gestionarea erorilor fără excepții."

### Skill rulat
- `.claude/skills/cqrs/SKILL.md` (v1.0)

### Ce a fost generat inițial & Probleme observate (Before)
- **Problema 1 (Tratare erori):** La rularea inițială, `DeleteSessionCommandHandler` și `DeleteTutorCommandHandler` aruncau `KeyNotFoundException` în loc să returneze `Result.Failure(Error.NotFound(...))`.
- **Problema 2 (Mapare status HTTP):** Endpoints foloseau blocuri repetitive de `if (result.IsFailure)` fără o metodă unificată de mapare (de exemplu `Match` sau extension method `ToHttpResult()`), ceea ce lăsa loc pentru inconsistențe între statusurile `400`, `404` și `409`.
- **Problema 3 (Separare Query / Command):** Interfețele generice nu ofereau o metodă clară de înregistrare automată în DI a tuturor handlerelor (`AddCqrsHandlers()`).

### Modificări aduse în skill (`SKILL.md`) (After v1.0)
- Adăugat în regulile skill-ului obligativitatea utilizării helperului `Match` / `ToHttpResult()` pe tipul `Result` / `Result<T>` pentru maparea directă la statusurile `TypedResults.Ok`, `TypedResults.Created`, `TypedResults.NoContent`, `TypedResults.NotFound`, `TypedResults.BadRequest`, `TypedResults.Conflict`.
- Explicitarea interdicției aruncării oricărei excepții în handlers; toate returnează `Result` sau `Result<T>`.
- Adăugată regula de înregistrare a handlerelor în DI prin metode de extensie (`services.AddCqrs()`).

---

### Prompt Iterativ (Code Review & Rafinare Skill)
> "Poti verifica ce este implementat si nu este salvat pe github? Fa un code review si asigura-te ca totul din acest task este implementat si ca este implementat IN MOD CORECT. Exista vreo cale de imbunatatire pentru cqrs skill, conform rezultatelor generate?"

### Probleme identificate în `SKILL.md` vs codul real (Before v1.1)
- **Problema 4 (Discrepanță constrângeri generice):** În codul real, `ICommandHandler` și `IQueryHandler` aveau constrângeri explicite pe interfețe marker (`where TCommand : ICommand<TResponse>`, `where TCommand : ICommand`, `where TQuery : IQuery<TResponse>`). În snippet-ul din `SKILL.md`, comenzile erau definite fără marker interfaces (`public record CreateTutorCommand(...)`), ceea ce genera erori de compilare la aplicarea skill-ului pe noi agregate.
- **Problema 5 (Boilerplate în Endpoints):** Exemplul de endpoint din `SKILL.md` continua să folosească blocul manual `result.Match(...)` cu switch inline în loc de apelul mult mai compact `result.ToHttpResult(...)`.
- **Problema 6 (Standardizare erori & structură DI):** În `SKILL.md` lipseau exemple pentru `ICommand` fără payload (delete/update), maparea structurată `{ error.Code, error.Description }` și exemplul de configurare DI centralizată în `DependencyInjection.cs`.

### Modificări aduse în skill (`SKILL.md`) (After v1.1)
- Actualizate toate semnăturile și constrângerile generice în `SKILL.md` pentru a include interfețele marker `ICommand`, `ICommand<TResponse>` și `IQuery<TResponse>`.
- Înlocuit switch-ul manual din exemplul de endpoint cu apelul fluent `result.ToHttpResult(response => TypedResults.Created(...))` și `result.ToHttpResult()`.
- Adăugate exemple complete pentru toate tipurile de use-case-uri:
  1. Command cu payload de răspuns (`CreateTutor` -> 201 Created).
  2. Command fără payload (`DeleteTutor` -> 204 NoContent).
  3. Query (`GetTutorById` -> 200 OK).
  4. Înregistrare DI consolidată în `Features/DependencyInjection.cs` (`AddCqrs`).
- Adăugată regula de propagare a parametrului `CancellationToken cancellationToken = default`.

---

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
- **Problema 7 (Tratare completă excepții de domeniu în Handlers):** În `UpdateTutorCommandHandler`, era prinsă doar `ArgumentException`, nu și `InvalidOperationException` (pentru conflicte de stare), generând potențial excepții nearuncate prin Result.

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

### Prompt Iterativ 2 (Regenerare cod conform noilor specificații din Skill v1.1)
> "Reciteste skill-ul si regenereaza codul, conform specificatiilor noi din skill"

### Verificare și regenerare (After v1.1)
- Verificat întregul arbore de clase din `Features/Tutors` și `Features/Sessions` față de regulile actualizate din `SKILL.md`.
- Regenerat și completat `UpdateTutorCommandHandler` pentru tratarea exhaustivă atât a `ArgumentException` (Validation) cât și a `InvalidOperationException` (Conflict).
- Confirmat că toate comenzile și interogările respectă marker interfaces (`ICommand`, `ICommand<TResponse>`, `IQuery<TResponse>`), că toți handlerii propagă `CancellationToken cancellationToken = default` și că toate endpoint-urile folosesc extensia fluentă `result.ToHttpResult(...)`.

---

## 2. Skill `code-review`: Static Analysis & Architecture Review

### Prompt inițial
> "Creează un skill Claude pentru code review static în `.claude/skills/code-review/SKILL.md`. Skill-ul trebuie să verifice conformitatea arhitecturii TutorMatch, regulile CQRS, invariantele de domeniu și calitatea codului, generând un raport de review structurat."

### Skill rulat
- `.claude/skills/code-review/SKILL.md` (v1.0)

### Ce a fost generat inițial & Probleme observate (Before)
- **Problema 1 (Tentativa de modificare a codului / violare read-only):** La execuțiile inițiale, agentul sau skill-ul încerca să propună sau să aplice direct editări în fișierele de cod sursă C# (`.cs`), încălcând separarea responsabilităților din echipă (sarcinile de persistență SQLite, teste unitare și de integrare fiind atribuite colegilor prin Issue-urile #22, #23 și #24).
- **Problema 2 (Categorii arbitrare și nestandardizate):** Defectele identificate erau grupate sub denumiri vagi și ad-hoc (ex: `Code Smell`, `Architecture Bug`, `Performance`, `Formatting`), fără o taxonomie predictibilă legată de arhitectura vertical slice și specificațiile din `cqrs/SKILL.md`.
- **Problema 3 (Format nestructurat și lipsa sortării după severitate):** Raportul generat nu avea un sumar cantitativ unificat, iar problemele minore de stil erau amestecate aleatoriu printre defecte critice de concurență sau invariante rupte.
- **Problema 4 (Numere de linie imprecise sau halucinate):** Recomandările citau căi de fișiere fără precizarea liniei sau cu numere de linie aproximative, îngreunând auditul manual și navigarea în IDE.
- **Problema 5 (Halucinații pe cod conform):** Tendința de a raporta "probleme fictive" pe porțiuni de cod conforme, doar din dorința de a genera output în tabel.

### Modificări aduse în skill (`SKILL.md`) (After v1.0)
- **Comportament strict Read-Only garantat:** Inclusă o regulă de bază absolută care interzice explicit crearea, modificarea sau ștergerea fișierelor sursă (`.cs`, `.csproj`). Orice remediere este exprimată exclusiv textual în coloana `Proposed fix`.
- **Format de ieșire unificat și obligatoriu:**
  1. O singură linie inițială de sumar: `Summary: X issues found (Y Critical, Z Major, W Minor)`.
  2. Un singur tabel Markdown cu exact 6 coloane: `| # | File:Line | Severity | Category | Problem | Proposed fix |`.
- **Severități strict definite (exact 3 niveluri):** `Critical`, `Major`, `Minor`, cu definiții precise pentru fiecare palier de risc.
- **Categorii fixe (exact 6 categorii):** `slice boundaries`, `CQRS violations`, `domain invariants`, `test quality`, `naming`, `error handling`, ancorate în realitatea arhitecturii `TutorMatch` și `cqrs/SKILL.md`.
- **Sortare descrescătoare obligatorie:** Rândurile din tabel sunt întotdeauna sortate: `Critical` -> `Major` -> `Minor`.
- **Regulă anti-halucinație și citare exactă `File:Line`:** Fiecare problemă raportată trebuie să citeze o cale relativă reală și un număr de linie existent fizic în fișier. Dacă nu există defecte, se raportează explicit `Summary: 0 issues found (0 Critical, 0 Major, 0 Minor)`.

---

## 3. Skill `sqlite-persistence`: SQLite & EF Core Integration (Issue #22)

> [!NOTE]
> *Secțiune rezervată pentru centralizarea prompt-urilor, rulărilor și iterațiilor Before/After aferente colegului responsabil de persistența SQLite și EF Core (Issue #22).*

### Prompt inițial
*(Urmează a fi completat la integrarea PR-ului colegului)*

### Skill rulat
*(Urmează a fi completat la integrarea PR-ului colegului)*

### Ce a fost generat inițial & Probleme observate (Before)
*(Urmează a fi completat la integrarea PR-ului colegului)*

### Modificări aduse în skill (`SKILL.md`) (After)
*(Urmează a fi completat la integrarea PR-ului colegului)*

---

## 4. Skill `unit-testing`: Unit Tests with xUnit and FluentAssertions (Issue #23)

> [!NOTE]
> *Secțiune rezervată pentru centralizarea prompt-urilor, rulărilor și iterațiilor Before/After aferente colegului responsabil de testele unitare (Issue #23).*

### Prompt inițial
*(Urmează a fi completat la integrarea PR-ului colegului)*

### Skill rulat
*(Urmează a fi completat la integrarea PR-ului colegului)*

### Ce a fost generat inițial & Probleme observate (Before)
*(Urmează a fi completat la integrarea PR-ului colegului)*

### Modificări aduse în skill (`SKILL.md`) (After)
*(Urmează a fi completat la integrarea PR-ului colegului)*

---

## 5. Skill `integration-testing`: Integration Tests with WebApplicationFactory (Issue #24)

> [!NOTE]
> *Secțiune rezervată pentru centralizarea prompt-urilor, rulărilor și iterațiilor Before/After aferente colegului responsabil de testele de integrare (Issue #24).*

### Prompt inițial
*(Urmează a fi completat la integrarea PR-ului colegului)*

### Skill rulat
*(Urmează a fi completat la integrarea PR-ului colegului)*

### Ce a fost generat inițial & Probleme observate (Before)
*(Urmează a fi completat la integrarea PR-ului colegului)*

### Modificări aduse în skill (`SKILL.md`) (After)
*(Urmează a fi completat la integrarea PR-ului colegului)*

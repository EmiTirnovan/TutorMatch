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
- **Problema 6 (Standardizare erori & structură DI):** În `SKILL.md` lipseau exemple pentru `ICommand` fără payload (delete/update), maparea structurată `{ error.Code, error.Description }` și exemplul de configurare DI centralizată în `DependencyInjection.cs`.\n- **Problema 7 (Tratare completă excepții de domeniu în Handlers):** În `UpdateTutorCommandHandler`, era prinsă doar `ArgumentException`, nu și `InvalidOperationException` (pentru conflicte de stare), generând potențial excepții nearuncate prin Result.

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

## 2. Skill `efcore-sqlite-repositories`: Persistență SQLite prin EF Core fără modificarea contractelor

### Prompt inițial

> "Creează skill-ul Claude `.claude/skills/efcore-sqlite-repositories/SKILL.md` pentru a înlocui implementările repository-urilor in-memory (`InMemoryTutorRepository`, `InMemorySessionRepository`) cu implementări reale bazate pe EF Core și SQLite, păstrând neschimbate interfețele existente și izolarea completă a folderului `Features/`. Configurează DbContext-ul `TutorMatchDbContext`, mapările fluente pentru Value Objects (`DateRange`), colecții complexe, optimistic concurrency (`Version`), relațiile dintre entități, înregistrarea în DI cu suport pentru SQLite in-memory, și generează codul conform DoD."

### Skill rulat

- `.claude/skills/efcore-sqlite-repositories/SKILL.md` (v1.0)

### Ce a fost generat inițial & Probleme observate (Before)

- **Problema 1 (Cartesian Explosion la interogări multiple collections):** La interogarea tutorilor cu colecțiile `Availability` și `TutorSubjects`, EF Core genera un warning (`MultipleCollectionIncludeWarning`) cauzat de evaluarea într-un singur query SQL.
- **Problema 2 (Compatibilitate teste de regresie in-memory):** Testele unitare existente din `InMemoryTutorRepositoryTests` și `InMemorySessionRepositoryTests` apelau direct metodele de extensie `AddTutorInfrastructure()` și `AddSessionInfrastructure()` verificând înregistrarea implementărilor Singleton in-memory. Înlocuirea brutală a metodelor ar fi dus la eșecul testelor de regresie.
- **Problema 3 (Rularea migrărilor la startup fără eroare de Scoped Service):** Apelul direct din `Program.cs` către `app.Services.GetRequiredService<TutorMatchDbContext>()` ar fi eșuat în mediul de dezvoltare din cauza rezolvării unui serviciu `Scoped` din rădăcina DI container-ului (`ValidateScopes`).

### Modificări aduse în skill (`SKILL.md`) și cod (After v1.0)

- **Rezolvare Problema 1:** Adăugat `.AsSplitQuery()` în `SqliteTutorRepository.GetById()` și `SqliteTutorRepository.GetAll()` pentru a preveni explozia carteziană și avertismentele EF Core.
- **Rezolvare Problema 2:** În `DependencyInjection.cs`, s-au păstrat metodele `AddTutorInfrastructure()` și `AddSessionInfrastructure()` marcate ca `[Obsolete]` pentru compatibilitate cu testele de regresie in-memory, în timp ce persistența principală a fost expusă prin `AddSqliteRepositories()` și `AddSqliteInMemoryRepositories()`.
- **Rezolvare Problema 3:** În `Program.cs`, migrările și inițializarea datelor demo au fost plasate într-un `using (var scope = app.Services.CreateScope())`, asigurând aplicarea automată la startup fără conflicte de scope.
- **Izolare strictă respectată:** Niciun fișier din `Features/` nu a fost modificat; interfețele `ITutorRepository` și `ISessionRepository` au rămas identice.
- **Toate testele trecute:** 119 teste rulate cu succes (`dotnet test`).

## 3. Skill `integration-testing`: End-to-End Testing cu SQLite In-Memory și WebApplicationFactory

### Prompt inițial

> "Folosește skill-ul `integration-testing` pentru a genera testele de integrare pentru slice-ul `CreateTutor` din folderul `Features/Tutors/CreateTutor`."

### Skill rulat

- `.claude/skills/integration-testing/SKILL.md` (v1.0)

### Ce a fost generat inițial & Probleme observate (Before)

- **Problema 1 (Halucinații în DTO-uri și erori):** AI-ul a inventat proprietăți pentru `CreateTutorRequest` (`new List<Guid>()`) și a ghicit că erorile de domeniu sunt returnate ca simplu text (ex: `body.Should().Contain("Tutor.Validation")`), fără să cunoască mapping-ul real din endpoint-uri.
- **Problema 2 (Eroare 500 - Foreign Key Constraint):** La testul de succes pentru `CreateTutor`, salvarea în SQLite a crăpat cu `SQLite Error 19: 'FOREIGN KEY constraint failed'` deoarece request-ul folosea un `SubjectId` care nu exista fizic în tabelul `Subjects`.
- **Problema 3 (Eroare de compilare CS8604 / DDD Encapsulation):** La încercarea de a verifica salvările în baza de date, Claude a folosit `.Include("_subjects")`. EF Core a aruncat `InvalidIncludePathError` pentru că `_subjects` este un câmp privat, protejat de încapsularea Domain-Driven Design.
- **Problema 4 (Leak de date între teste - Numărătoare incorectă):** Testul de failure (400) verifica `tutorsCount.Should().Be(0)`, dar eșua găsind 1 tutore, deoarece testul rula pe aceeași conexiune deschisă în care primul test inserase deja date. Similar pentru `GetTutors`, unde datele globale de seed din `Program.cs` stricau aserțiunile de tip `HaveCount(1)` sau `BeEmpty()`.
- **Problema 5 (Enum Hallucination):** La `CreateSession`, a apărut eroarea de compilare `CS0117: 'SessionStatus' does not contain a definition for 'Scheduled'` pentru că AI-ul a ghicit o valoare în loc să folosească enum-ul real.

### Modificări aduse în skill (`SKILL.md`) (After v1.0, v1.1, v1.2)

- **Anti-Hallucination & Context Rules:** S-a introdus o secțiune strictă care interzice ghicirea DTO-urilor, a rutelor, a proprietăților și a membrilor de Enum.
- **Database Seeding (Foreign Keys):** Adăugată regula obligatorie ca orice ID din Request (ex: `SubjectId`, `TutorId`) să fie salvat explicit în baza de date în faza de `Arrange`, prevenind erorile de constrângere SQLite.
- **DDD Encapsulation & EF Core Includes:** Interzicerea strictă a folosirii metodei `.Include()` pe proprietăți private. Verificarea relațiilor se face interogând direct tabelele de legătură (ex: `db.Set<TutorSubject>()`).
- **Failure Assertions & Error Parsing:** S-a forțat citirea erorilor din răspuns cu `ReadAsStringAsync()` în loc de o deserializare oarbă în `ProblemDetails`, pentru a acomoda formatul custom din CQRS.
- **State Verification & Seed Independence:** În testele de eșec sau de listare, aserțiunile nu mai verifică un număr absolut de elemente (evitând interferențele cu seed-ul global sau leak-urile între teste), ci folosesc `Contains` pentru datele specifice testului.

## 4. Skill `unit-testing`: Unit Tests pentru Domenii si Handlere

### Prompt Initial:

> "Genereaza-mi unit testele pentru domeniile Sessions, Tutors si Student, dar si pentru handlerele din Features de la Sessions si Tutors. Foloseste claude skill ul /unit-testing."

### Skill rulat:

- `.claude/skills/unit-testing/SKILL.md`

### Output inițial (Greșit):

AI-ul a generat corect testele noi, dar a ignorat complet testele legacy deja existente din folderul Domain. Acestea au rămas cu assert-uri standard, deși skill-ul cerea explicit FluentAssertions.

### Ce am schimbat și de ce (Iterația 1 - Legacy Tests):

- _Before:_ Nu exista nicio regulă despre cum să trateze testele scrise anterior, așa că modelul le-a lăsat intacte.
- _After:_ Am adăugat secțiunea `## Existing Tests & Legacy Code` cu regula `Conditional Refactoring`: _"If existing tests violate ANY rule... you MUST completely refactor them... If existing tests already fully comply with these rules, DO NOT rewrite them."_
- _De ce:_ Pentru a forța AI-ul să identifice și să refactorizeze codul legacy aducându-l la standardele FluentAssertions 7.\*. Am adăugat condiția de a păstra codul deja valid ca o deducție logică preventivă, pentru a nu lăsa AI-ul să rescrie inutil aceleași teste la fiecare rulare viitoare a prompt-ului.

### Rularea 2 (Greșeală de structură):

La re-rulare, AI-ul a refactorizat testele vechi, dar a încălcat structura Vertical Slices. În loc să creeze câte un fișier de test separat (mirror 1:1) pentru fiecare handler de producție, a comasat toate testele pentru un feature într-un fișier generic unic (ex: `SessionHandlerTests.cs`).

### Ce am schimbat și de ce (Iterația 2 - Strict 1:1 Mapping):

- _Before:_ Regula de structură a folderelor exista, dar nu interzicea explicit comasarea testelor.
- _After:_ Am adăugat regula `## Folder & File Structure (Strict 1:1 Mapping)` care specifică: _"You MUST create exactly one test file for every production file being tested. NEVER compress, combine, or group tests..."_. Am inclus și un prim exemplu de mapare.
- _De ce:_ Pentru a mă asigura că testele respectă arhitectura independentă a slice-urilor, unde fiecare comandă/interogare are propriul său context de testare izolat.

### Rularea 3 (Greșeală de path-uri):

Am dat din nou prompt-ul. AI-ul a aplicat regula de 1:1 mapping, însă pentru fișierul `DeleteSessionCommandHandlerTests.cs` a halucinat calea absolută și l-a creat direct în folderul meu de sistem `/home/cosmin`, în loc să îl pună în interiorul proiectului, alături de celelalte.

### Ce am schimbat și de ce (Iterația 3 - Workspace-Anchored Paths):

- _Before:_
  ```markdown
  - Exact Folder Mirroring: The directory structure of the test project must faithfully mirror the directory structure of the production code.
  - _Example:_ Tests for `Features/Sessions/CreateSession/CreateSessionCommandHandler.cs` must reside exactly in `Features/Sessions/CreateSession/CreateSessionCommandHandlerTests.cs`.
  ```
- _After:_ Am extins exemplul într-unul "workspace-anchored", specificând clar cum arată paths de la root-ul proiectului (`TutorMatch/src/...` -> `TutorMatch/tests/...`). Am adăugat și regula absolută: _"Never create files outside the current workspace root. If a path cannot be resolved inside the workspace, stop and ask for clarification."_
- _De ce:_ Pentru a fixa AI-ul în limitele directorului curent și a preveni scrierea de fișiere oriunde altundeva pe sistemul de operare.

### Iterațiile Ulterioare: Corectarea halucinațiilor și a "lenei" agentului

- **Problema:** Deși promptul inițial era clar, coverage-ul se bloca mereu între 80% și 98%. Agentul declara victoria și se oprea, susținând că a atins 100%, deși raportul arăta altceva.
- **De ce eșua agentul (Cauze observate):**
  1. _Limitarea textului generat:_ Încercând să analizeze și să scrie zeci de teste dintr-un foc, agentul pur și simplu "obosea" (își atingea limita de generare a textului) și se oprea brusc, omițând fișiere (ex: ratase excluderea pentru `UpdateSessionRequest`).
  2. _Ignora liniile simple de cod:_ Agentul era foarte atent să acopere deciziile logice (`if`/`else`, `throw`), dar uita complet să scrie teste pentru liniile secvențiale unde doar se copiau date dintr-o parte în alta.
  3. _Trăgea concluzii pripite:_ Când rula testele în terminal și vedea că toate trec cu succes (fără erori), agentul halucina direct că a terminat totul cu 100%, refuzând să mai citească raportul efectiv de coverage pentru confirmare.
- **Rezolvarea (Modificări în SKILL.md):**
  Am structurat skill-ul sub forma unei strategii de execuție în pași clari:
  - _Step 1 (The Exclusion Sweep):_ Am instruit agentul să excludă din start fișierele fără logică de business (DTO-uri, infrastructură, configurări) pentru a nu distorsiona calculul matematic al coverage-ului.
  - _Step 3 (Self-Verification):_ Am impus o regulă strictă de validare. Agentul nu mai are voie să considere testarea completă doar pentru că nu există erori în consolă. Trebuie să verifice efectiv raportul generat și l-am instruit explicit să scrie teste și pentru operațiunile secvențiale (Line-Coverage Blindspot), forțându-l să acopere întregul fișier, nu doar ramurile condiționale.
- **Rezultat Final:** Fiind ghidat să lucreze organizat și să se auto-verifice strict pe baza raportului HTML/XML, agentul a acoperit manual ultimele rânduri lipsă și a ajuns la 100% coverage absolut.

---

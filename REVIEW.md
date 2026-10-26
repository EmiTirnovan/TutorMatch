# Code Review & Static Analysis Report (REVIEW.md)

Acest document centralizează rezultatele evaluării statice automate a codului, executată în mod strict **read-only** pe baza regulilor și convențiilor definite în [`.claude/skills/code-review/SKILL.md`](file:///.claude/skills/code-review/SKILL.md).

---

## 1. Review pe repository-ul propriu (`TutorMatch`)

> [!NOTE]
> Această analiză a fost actualizată automat în urma integrării (merge cu `main`) a PR-urilor pentru Issue #22 (Persistență SQLite / EF Core), Issue #23 (Teste unitare) și Issue #24 (Teste de integrare).
> În urma integrării, lipsa testelor unitare pe handlere și convențiile de denumire a metodelor din `DateRangeTests` au fost rezolvate cu succes de către colegi.

Summary: 6 issues found (3 Critical, 2 Major, 1 Minor)

| # | File:Line | Severity | Category | Problem | Proposed fix |
|---|-----------|----------|----------|---------|--------------|
| 1 | `TutorMatch/src/TutorMatch.Api/Features/Sessions/CreateSession/CreateSessionCommandHandler.cs:28` | Critical | domain invariants | Handlerul instanțiază și adaugă sesiunea direct în repository (`_repository.Add(session)`) fără a verifica invariantul de domeniu R1 ("Sesiunile aceluiași tutore nu se suprapun niciodată"). Astfel, pot fi salvate sesiuni cu intervale suprapuse pentru același tutore. | Înainte de salvare, se interoghează repository-ul pentru sesiunile existente ale tutorelui (`command.TutorId`) și se verifică dacă vreuna se suprapune cu noul interval (`existing.Interval.Overlaps(interval)`). Dacă există conflict, se returnează `Result<CreateSessionResponse>.Failure(Error.Conflict("Session.Conflict", "Slotul selectat se suprapune cu o altă sesiune a tutorelui."))`. |
| 2 | `TutorMatch/src/TutorMatch.Api/Domain/Students/Student.cs:48` | Critical | domain invariants | Metoda `BookSession` din agregatul `Student` verifică doar suprapunerea intervalului pe student, dar nu verifică invariantul de domeniu R4 stabilit în documentație ("Un student are maxim 2 sesiuni pe zi"). | Se adaugă o verificare în `BookSession`: dacă `_sessions.Count(s => s.Status == SessionStatus.Booked && s.Interval.Start.Date == interval.Start.Date) >= 2`, se aruncă `InvalidOperationException("Studentul are deja numărul maxim de 2 sesiuni permise în această zi.")`. |
| 3 | `TutorMatch/src/TutorMatch.Api/Domain/Students/Student.cs:19` | Critical | domain invariants | Constructorul entității `Student` acceptă parametri impliciți `name = ""` și `email = ""` și nu validează prezența caracterelor non-whitespace, permițând crearea unui student invalid cu nume și email vide. | Se elimină valorile implicite vide și se adaugă validările `if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(...)` și `if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException(...)`, similar cu verificările din agregatul `Tutor`. |
| 4 | `TutorMatch/src/TutorMatch.Api/Features/Tutors/CreateTutor/CreateTutorRequest.cs:3` | Major | slice boundaries | DTO-ul `DateRangeDto` este declarat în `CreateTutorRequest.cs` (în interiorul slice-ului `CreateTutor`), dar este importat și folosit direct în alt vertical slice (`GetTutorByIdQueryHandler.cs:4` și `GetTutorByIdResponse.cs:1`), cuplând două slice-uri independente. | Se mută `DateRangeDto` într-o zonă comună de contracte/DTO-uri (`Common/Dtos/DateRangeDto.cs` sau `Features/Tutors/Contracts/`) ori se declară un DTO specific fiecărui slice pentru a asigura izolarea verticală completă. |
| 5 | `TutorMatch/src/TutorMatch.Api/Features/Sessions/UpdateSession/UpdateSessionCommandHandler.cs:29` | Major | error handling | Apelul `_repository.Update(session)` poate arunca `KeyNotFoundException` în caz de ștergere concurentă între citire și scriere (vezi `InMemorySessionRepository.cs:43`), însă handlerul prinde doar `ArgumentException` și `InvalidOperationException`. O excepție concurentă ar duce la un crash HTTP 500 necontrolat în loc de Result funcțional. Același defect apare și în `UpdateTutorCommandHandler.cs:28`. | Se adaugă un bloc `catch (KeyNotFoundException ex)` în handler care să returneze `Result.Failure(Error.NotFound("Session.NotFound", ex.Message))` conform cerințelor din `cqrs/SKILL.md`. |
| 6 | `TutorMatch/src/TutorMatch.Api/Features/Tutors/GetTutors/GetTutorsResponse.cs:3` | Minor | naming | Numele fișierului este `GetTutorsResponse.cs`, dar tipul record declarat în fișier este `TutorSummaryResponse`, creând o neconcordanță între numele fișierului și al tipului, precum și o deviație față de convenția `<UseCase>Response` utilizată în `GetSessionsResponse.cs`. | Se redenumește tipul în `GetTutorsResponse` (sau se declară un record `GetTutorsResponse` ce împachetează colecția) și se actualizează referințele din `GetTutorsQuery`, `GetTutorsQueryHandler`, `GetTutorsEndpoint` și `DependencyInjection.cs`. |

### Probleme remediate prin PR-urile colegilor (#22, #23, #24)
- **`test quality` (fost #6):** Lipsa testelor unitare pentru handlerele CQRS a fost complet rezolvată prin adăugarea suitei complete de teste în `TutorMatch.Tests/Features/` (acoperire de 100% obținută pe handlere).
- **`test quality` (fost #8):** Convenția de denumire a metodelor de testare din `DateRangeTests.cs` a fost refactorizată conform standardului BDD / FluentAssertions (`Given_..._When_..._Then_...`).

---

## 2. Review pe repository-ul colegului / echipei alocate

> [!IMPORTANT]
> Această secțiune este pregătită pentru populare la momentul rulării skill-ului `.claude/skills/code-review/SKILL.md` pe repository-ul echipei partenere alocate pentru cross-review.

Summary: 0 issues found (0 Critical, 0 Major, 0 Minor) *(Pending execution on peer repository)*

| # | File:Line | Severity | Category | Problem | Proposed fix |
|---|-----------|----------|----------|---------|--------------|
| - | *TBD* | *TBD* | *TBD* | *Secțiune pregătită pentru analiza statică a codului din repository-ul colegului* | *Urmează a fi completat conform formatului skill-ului `code-review`* |

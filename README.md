# TutorMatch – Descriere proiect și împărțirea pe module

## Rulare rapidă și testare

### Testare manuală (Postman)

1. Pornește aplicația din rădăcina repository-ului rulând comanda: `dotnet run --project TutorMatch/src/TutorMatch.Api` (API-ul pornește pe `http://localhost:5245`).
2. Deschide Postman și importă colecția din `/postman/TutorMatch.postman_collection.json` folosind butonul **Import**.
3. Rulează cererile direct din **Run collection** sau individual (variabila `{{baseUrl}}` este deja preconfigurată la `http://localhost:5245`).

### Comenzi CLI pentru Build, Testare și Raport de Acoperire (DoD)

Toate comenzile de mai jos se execută din rădăcina repository-ului:

a. **Restaurare dependințe și build:**
```bash
dotnet build TutorMatch/TutorMatch.sln
```

b. **Rulare teste unitare (fără cele de integrare):**
```bash
dotnet test TutorMatch/TutorMatch.sln --filter "Category!=Integration"
```

c. **Rulare teste de integrare:**
```bash
dotnet test TutorMatch/TutorMatch.sln --filter "Category=Integration"
```

d. **Generare raport de acoperire a codului cu Coverlet:**
```bash
dotnet test TutorMatch/TutorMatch.sln --collect:"XPlat Code Coverage"
```

e. **Generare și vizualizare raport HTML cu ReportGenerator:**
```bash
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```
*Cale de vizualizare raport:* Deschide fișierul `coveragereport/index.html` în orice browser web.

**Echipa:** Emi, Razvan, Cosmin, Valentin, Andrei  
**Stadiu document:** împărțirea pe module (sprint-urile se stabilesc ulterior)

---

## 1. Despre proiect

**TutorMatch** este o platformă de tutoriat între colegi: un student din anul 3 ajută un student din anul 1 la algoritmi. Diferența față de o listă clasică de materii este că studentul își scrie problema în text liber (ex. *„am nevoie de ajutor cu pointerii”*), iar un modul AI (embeddings prin Ollama) găsește tutorele potrivit.

### Funcționalități

**Tutori**
- Înregistrare tutor: bio, materii, tarif orar, disponibilitate.
- Actualizare disponibilitate (listă de intervale `DateRange`).
- Vizualizare profil tutor.

**Căutare**
- Căutare clasică cu filtre (materie, preț, disponibilitate).
- Căutare semantică: `GET /api/tutors/semantic-search?q=...` întoarce top 5 tutori după similaritate cosinus.
- Fallback automat pe căutarea clasică dacă Ollama nu răspunde, cu `mode: "keyword"` în răspuns.

**Sesiuni**
- Rezervare sesiune pe un interval.
- Anulare sesiune (gratuit cu cel puțin 12h înainte, altfel `LateCancelled`).
- Marcare sesiune ca finalizată.
- Listarea sesiunilor proprii.

**Rating**
- Se poate acorda doar după o sesiune `Completed`, o singură dată per sesiune.

### Reguli de domeniu (invariante)
| # | Regulă |
|---|--------|
| R1 | Sesiunile aceluiași tutore nu se suprapun niciodată (`DateRange.Overlaps`). |
| R2 | Două rezervări simultane pe același slot se rezolvă cu `Version` (optimistic concurrency) și răspuns **409**. |
| R3 | Anulare gratuită cu ≥ 12h înainte de start; mai târziu → `LateCancelled`. |
| R4 | Un student are maxim 2 sesiuni pe zi. |
| R5 | Rating doar după `Completed`, o singură dată per sesiune. |

### Cele 9 slice-uri + modulul AI
`RegisterTutor`, `UpdateAvailability`, `SearchTutors`, `GetTutorById`, `BookSession`, `CancelSession`, `CompleteSession`, `RateSession`, `GetMySessions` + **SemanticSearch** (Ollama).

---

## 2. Principiu de împărțire

Fiecare membru deține un **modul vertical end-to-end**: backend (slice-uri + domeniu + persistență), frontend (pagini/componente), teste (unit, integrare, UI/E2E unde are sens) și documentație scurtă. Astfel toți exersăm tot lanțul, nu doar un strat.

Modulele au dificultate echilibrată și fiecare conține câte un subiect „greu” din care învățăm ceva diferit: modelare de domeniu, concurență, reguli de timp, căutare, AI.

---

## 3. Modulele

### 👤 Emi – Modulul **Tutori & Disponibilitate** (fundația domeniului)

**Slice-uri:** `RegisterTutor`, `UpdateAvailability`, `GetTutorById`

**Backend**
- Agregatul `Tutor` (bio, materii, tarif orar, disponibilitate).
- Value object-ul **`DateRange`** cu `Overlaps`, `Contains`, validări (start < end) – folosit de toată echipa.
- Validarea disponibilității (fără intervale suprapuse în aceeași listă).
- Persistență SQLite pentru tutori și disponibilitate.
- Publicarea unui eveniment/notificare „TutorSaved” (la înregistrare și la update de profil), pe care îl consumă modulul AI.

**Frontend**
- Formular de înregistrare tutor.
- Editor de disponibilitate (adăugare/ștergere intervale).
- Pagina de profil tutor.

**Teste**
- Unit tests foarte solide pe `DateRange` (suprapuneri, margini, intervale adiacente).
- Teste de validare pentru Tutor.
- Teste de integrare pe endpoint-urile celor 3 slice-uri.

**Ce învață:** modelare de domeniu (DDD), value objects, validări, publicare de evenimente.

> ⚠️ **Dependență critică:** `DateRange` și agregatul `Tutor` trebuie livrate **primele**, pentru că Razvan și Cosmin se bazează pe ele. Emi publică o versiune minimă în primele zile.

---

### 📅 Razvan – Modulul **Rezervări & Concurență**

**Slice-uri:** `BookSession`

**Backend**
- Agregatul `Session` (tutor, student, interval, status, `Version`).
- Regula R1: fără suprapuneri pe același tutore.
- Regula R2: **optimistic concurrency** cu `Version`, răspuns **409** la conflict.
- Regula R4: maxim 2 sesiuni pe zi per student.
- Verificarea că intervalul cerut se încadrează în disponibilitatea tutorelui.
- Persistență SQLite pentru sesiuni, inclusiv coloana de versiune.

**Frontend**
- Selector de slot / calendar pe profilul tutorelui.
- Fluxul de rezervare + tratarea clară a erorilor (409 „slot ocupat”, limită zilnică atinsă).

**Teste**
- Unit tests pe regulile R1 și R4.
- **Test de concurență:** două rezervări în paralel pe același slot → una reușește, una primește 409.
- Teste de integrare end-to-end pe `BookSession`.

**Ce învață:** concurență, optimistic locking, invariante de domeniu, teste cu task-uri paralele.

---

### ⏱️ Cosmin – Modulul **Ciclul de viață al sesiunii**

**Slice-uri:** `CancelSession`, `CompleteSession`, `GetMySessions`

**Backend**
- Mașina de stări a sesiunii: `Booked → Cancelled | LateCancelled | Completed`.
- Regula R3: anulare gratuită cu ≥ 12h înainte, altfel `LateCancelled`.
- `CompleteSession`: tranziția către `Completed` doar când este permis (după/în timpul intervalului, doar din starea validă).
- `GetMySessions`: listare pentru student și pentru tutor, cu filtrare pe status.
- Abstracție de timp (`IClock` / `TimeProvider`) pentru testabilitate.

**Frontend**
- Pagina „Sesiunile mele” (viitoare / trecute / anulate).
- Butoane Anulează / Marchează finalizată.
- Avertisment în UI când anularea ar fi tardivă.

**Teste**
- Unit tests cu **ceas fals** pe toate marginile regulii de 12h (11h59, 12h00, 12h01).
- Teste pentru tranziții de stare invalide.
- Teste de integrare pe cele 3 slice-uri.

**Ce învață:** reguli bazate pe timp, state machines, testare deterministă cu ceas injectat.

---

### ⭐ Valentin – Modulul **Descoperire & Feedback** (căutare clasică + rating)

**Slice-uri:** `SearchTutors`, `RateSession`

**Backend**
- `SearchTutors`: filtre (materie, interval de preț, disponibilitate într-o dată/interval), paginare, sortare.
- Aceasta este **baza de comparație („keyword”)** pentru evaluarea AI și fallback-ul din SemanticSearch – deci contractul răspunsului se stabilește împreună cu Andrei.
- `RateSession`: R5 (doar după `Completed`, o singură dată per sesiune), scor 1–5 + comentariu opțional.
- Rating mediu afișabil pe profilul tutorelui (agregare).

**Frontend**
- Pagina de căutare cu filtre și listă de rezultate (card tutor).
- Componenta de rating (stele + comentariu) accesibilă din sesiunile finalizate.

**Teste**
- Teste pe combinații de filtre și cazuri limită (fără rezultate, filtre contradictorii).
- Teste pentru R5 (rating dublu, rating pe sesiune ne-finalizată).
- Teste de integrare + un test de UI pe fluxul de căutare.

**Ce învață:** interogări/filtre dinamice, paginare, reguli de unicitate, agregări; în plus contribuie la **etichetarea setului de evaluare** (vezi mai jos).

---

### 🤖 Andrei – Modulul **AI SemanticSearch (Ollama)**

**Slice-uri:** `SemanticSearch` (modul AI)

**Backend**
- Client Ollama (HttpClient) către `/api/embed` – **de verificat în documentația versiunii de Ollama folosite**.
- Handler care, la salvarea profilului (evenimentul din modulul lui Emi), generează embedding-ul pentru `bio + subiecte` și îl stochează în SQLite ca **BLOB**.
- Endpoint `GET /api/tutors/semantic-search?q=...`: embed pentru întrebare → similaritate cosinus pe toți tutorii în memorie → **top 5**.
- **Fallback:** dacă Ollama nu răspunde (timeout/eroare), se folosește `SearchTutors` iar răspunsul conține `mode: "keyword"` (altfel `mode: "semantic"`).
- Alegerea modelului: testat `bge-m3` vs `nomic-embed-text` pe întrebări reale (verificat nume, dimensiune, licență în biblioteca Ollama).

**Evaluare (livrabil de sine stătător)**
- 20–30 perechi întrebare → tutorele corect, etichetate manual (se completează cu ajutorul întregii echipe, coordonat de Andrei, cu baza de keyword de la Valentin).
- Metrica: „tutorele corect e în top 3”, comparată cu căutarea keyword.
- Un mic raport cu rezultatele și concluziile pe model.

**Frontend**
- Bară de căutare în text liber („Descrie problema ta…”).
- Afișarea rezultatelor + indicator vizibil al modului (`semantic` / `keyword`) și mesaj la fallback.

**Teste**
- Unit tests pentru similaritatea cosinus și serializarea vectorilor în BLOB.
- Client Ollama **fals** (fake) pentru teste de integrare, inclusiv scenariul de fallback.
- Scriptul/testul de evaluare (top-3 accuracy) rulabil automat.

**Ce învață:** embeddings, similaritate vectorială, integrare cu un serviciu extern instabil, rezilență (fallback), evaluarea unui sistem AI.

---

## 4. Sarcini transversale (care le ia cine)

| Sarcină | Responsabil principal | Susținere |
|---|---|---|
| Structura soluției, convenții de cod, setup repo | Emi | toți |
| Schema SQLite, migrări, date de test (seed) | Emi | Razvan |
| Identitatea utilizatorului (simplă: student / tutor, ex. header sau login minimal – de decis) | Cosmin | Emi |
| Contractul comun al răspunsurilor (erori, ProblemDetails, 409) | Razvan | Valentin |
| Layout frontend, navigare, componente comune | Valentin | Cosmin |
| CI (build + teste la fiecare PR) | Cosmin | – |
| Setup Ollama local + instrucțiuni de rulare | Andrei | – |
| README final + demo | toți | Andrei coordonează |

---

## 5. Dependențe între module

```
Emi (Tutor, DateRange) ──► Razvan (BookSession) ──► Cosmin (Cancel/Complete/MySessions) ──► Valentin (RateSession)
        │                                                                                           
        ├──► Andrei (embedding la TutorSaved)                                                       
        └──► Valentin (SearchTutors) ──► Andrei (fallback + baseline evaluare)
```

Pe scurt: **Emi livrează primul** `DateRange` + `Tutor`; Razvan are nevoie de ele pentru rezervări; Cosmin și Valentin (partea de rating) au nevoie de `Session`; Andrei are nevoie de evenimentul de la Emi și de `SearchTutors` de la Valentin. Până apar dependențele reale, fiecare lucrează cu stub-uri/fakes.

---

## 6. Reguli de lucru propuse (pentru a învăța toți de toate)

- **Code review încrucișat** în cerc: Emi → Razvan → Cosmin → Valentin → Andrei → Emi. Fiecare e obligat să citească cod dintr-un domeniu diferit.
- **Pair programming** pe părțile grele: concurența (Razvan + Cosmin), integrarea Ollama (Andrei + Emi), etichetarea setului de evaluare (toți).
- **Prezentare scurtă** a fiecărui modul în echipă la final, ca să înțeleagă toți restul sistemului.
- **Definition of Done** per modul: backend + frontend + teste trecute în CI + review făcut + README scurt al modulului.

---

## 7. De decis împreună (înainte de primul sprint)

1. Stack tehnologic (backend / frontend) – domeniul sugerează .NET + SQLite, frontend la alegere.
2. Cum modelăm utilizatorii (autentificare simplă sau doar rol + id).
3. Modelul de embeddings (`bge-m3` vs `nomic-embed-text`) – în funcție de limba întrebărilor.
4. Formatul comun al erorilor și al răspunsurilor API.
5. Cum împărțim sprint-urile (pe module sau pe etape: fundație → reguli → UI → AI → evaluare).

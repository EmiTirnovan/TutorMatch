# Raport tehnic: evaluarea căutării semantice TutorMatch

> **Stare:** metricile offline (deterministe) sunt măsurate și incluse mai jos. Metricile pentru `bge-m3` și
> `nomic-embed-text` **necesită rulare live cu Ollama** și sunt marcate „⏳ necesită rulare live”. Ollama nu rula
> pe mașina pe care a fost generat acest raport (`GET http://localhost:11434/api/tags` → timeout).
> Comanda exactă pentru completarea lor este în secțiunea [5](#5-rulare-live-bge-m3-vs-nomic-embed-text).

## 1. Ce se evaluează

Întrebare: pentru o problemă formulată liber de un student, apare tutorele potrivit pe primul loc (Top-1) sau în
primele trei rezultate (Top-3)? Comparăm:

| Strategie | Implementare evaluată |
|---|---|
| Semantic (`bge-m3`) | `SemanticSearchQueryHandler` real + `OllamaClient` real, model `bge-m3` |
| Semantic (`nomic-embed-text`) | idem, model `nomic-embed-text` |
| Căutare clasică pe cuvinte cheie | `SearchTutorsQueryHandler` (Discovery), apelat ca în fallback și în frontend: întreaga interogare ca filtru `subject` |
| Vectori lexicali bag-of-words (proxy offline) | același `SemanticSearchQueryHandler`, dar embedding-urile vin de la `DeterministicTextEmbedder` (hash de cuvinte, fără model) |
| Semantic cu Ollama căzut | `SemanticSearchQueryHandler` cu un client care aruncă excepție → fallback `keyword` |

## 2. Metodologie

**Set de date.** 30 de formulări realiste de probleme studențești, 11 tutori (`MockTutorPool`) din 11 domenii
(matematică, algoritmi/C++, fizică, engleză, chimie, web, ML, baze de date, germană, economie, biologie).
Sursa unică de adevăr este `SemanticSearchEvaluationTests.GroundTruthEvaluationSet`; fișierul frontend
`frontend/src/app/core/data/evaluation-cases.ts` are aceleași interogări, ID-uri și nume, iar testul
`Given_FrontendEvaluationCases_When_ComparedWithBackendGroundTruth_Then_QueriesAndExpectedTutorsMatch` pică dacă
cele două se desincronizează.

**Indexare.** Tutorii sunt salvați într-o bază SQLite in-memory, apoi indexați prin handler-ul de producție
`TutorSavedNotificationHandler`. Textul trimis la embedding are formatul de producție
`Name | Subjects | Bio` (materii deduplicate și sortate), de exemplu:

```text
Alexandru Radu | Algoritmi, C++, Informatica, POO | Software engineer si trainer. Ajut studentii cu algoritmi, ...
```

**Interogare.** Fiecare interogare trece prin `SemanticSearchQueryHandler` (similaritate cosinus, top 5), cu
bugetul de producție de 3,5 s pentru embedding-ul interogării și **fără cache** (fiecare interogare ajunge la model).

**Metrici.**

| Metrică | Definiție |
|---|---|
| Top-1 Accuracy | % interogări cu tutorele așteptat pe poziția 1 |
| Top-3 Accuracy | % interogări cu tutorele așteptat în primele 3 poziții |
| Fallback Rate | % interogări la care răspunsul a venit cu `mode: "keyword"` (timeout/eroare Ollama) |
| Latență medie / interogare | media timpului de execuție al handler-ului (in-process, fără HTTP) |

Acuratețea este măsurată end-to-end: dacă o interogare face fallback, se notează ce a returnat fallback-ul,
adică exact ce ar vedea studentul.

## 3. Rezultate offline (deterministe, măsurate)

Rulate în fiecare `dotnet test` de
`Given_OfflineEmbedder_When_ComparingStrategies_Then_VectorPipelineServesAllQueriesAndBeatsKeywordSearch`.

| Strategie | Interogări | Top-1 Accuracy | Top-3 Accuracy | Fallback Rate | Latență medie / interogare |
|---|---:|---:|---:|---:|---:|
| Vectori lexicali bag-of-words (proxy offline) | 30 | 80,0% | 96,7% | 0,0% | 2,9 ms |
| Căutare clasică pe cuvinte cheie (SearchTutors) | 30 | 0,0% | 0,0% | 0,0% | 0,8 ms |
| Semantic, Ollama căzut (fallback) | 30 | 0,0% | 0,0% | 100,0% | 0,7 ms |

Latențele de aici sunt in-process pe SQLite in-memory, fără model. Nu sunt reprezentative pentru Ollama.

Rata Top-3 a proxy-ului lexical: 29/30. Singura ratare este #28 („elasticitatea cererii și costul marginal …
microeconomie”), unde Laura Gheorghe iese pe locul 5. Interogarea nu are cuvinte în comun cu profilul ei, cu
excepția „microeconomie”. Ratări Top-1 (locul 2–3): #3, #6, #13, #18, #19.

## 4. Rezultate live: `bge-m3` vs `nomic-embed-text`

| Strategie | Interogări | Top-1 Accuracy | Top-3 Accuracy | Fallback Rate | Latență medie / interogare |
|---|---:|---:|---:|---:|---:|
| Semantic (`bge-m3`) | 30 | ⏳ necesită rulare live | ⏳ necesită rulare live | ⏳ necesită rulare live | ⏳ necesită rulare live |
| Semantic (`nomic-embed-text`) | 30 | ⏳ necesită rulare live | ⏳ necesită rulare live | ⏳ necesită rulare live | ⏳ necesită rulare live |
| Căutare clasică pe cuvinte cheie | 30 | 0,0% | 0,0% | 0,0% | ⏳ (re-măsurată la rularea live) |

După rulare, benchmark-ul scrie tabelul complet, plus rangul tutorelui așteptat pentru fiecare interogare și model,
în `docs/ai-evaluation-live-results.md`. Copiază de acolo cifrele în tabelul de mai sus.

## 5. Rulare live (`bge-m3` vs `nomic-embed-text`)

Din rădăcina repository-ului, în PowerShell:

```powershell
ollama pull bge-m3
ollama pull nomic-embed-text
# pornește serverul dacă nu rulează deja (aplicația desktop Ollama îl pornește automat)
ollama serve

cd TutorMatch
$env:TUTORMATCH_LIVE_EVAL = "1"
dotnet test --filter "FullyQualifiedName~SemanticSearchLiveBenchmarkTests" --logger "console;verbosity=detailed"
```

Variabile opționale: `OLLAMA_BASE_URL` (implicit `http://localhost:11434`), `TUTORMATCH_EVAL_MODELS`
(implicit `bge-m3,nomic-embed-text`), `TUTORMATCH_EVAL_OUTPUT` (implicit `docs/ai-evaluation-live-results.md`).
Dacă pe mașină există doar runtime-ul .NET 10 (proiectul țintește net8.0), adaugă `$env:DOTNET_ROLL_FORWARD = "Major"`.

Benchmark-ul face un apel de încălzire per model, cu timeout de 120 s, ca încărcarea la rece a modelului să nu
fie numărată drept fallback. Indexarea folosește același timeout. Interogările păstrează bugetul de producție de
3,5 s. Fără `TUTORMATCH_LIVE_EVAL=1`, testul apare ca *Skipped* în `dotnet test`.

## 6. Interpretare și limite

- **Căutarea clasică are 0% pe acest set.** `SearchTutors` caută interogarea întreagă ca subșir în numele
  materiilor (`Subject.Name.Contains(query)`, case-sensitive în SQLite). O frază ca „Am nevoie de ajutor cu
  analiza matematica…” nu este subșir al niciunei materii. Acesta este comportamentul real al fallback-ului
  și al filtrului din frontend, nu o configurare defavorabilă aleasă pentru test.
- **Setul de date este ușor lexical.** Proxy-ul bag-of-words ajunge la 96,7% Top-3 pentru că majoritatea
  interogărilor repetă cuvinte din bio sau din materii. Proxy-ul este deci o bază de comparație lexicală
  solidă: un model semantic aduce valoare reală pe acest set doar dacă îl depășește, mai ales la Top-1 și la
  cazuri ca #28 (parafraze fără cuvinte comune). Pentru o diferențiere mai clară, setul ar avea nevoie de mai
  multe parafraze fără suprapunere lexicală.
- **Fallback-ul funcționează determinist.** Cu Ollama indisponibil, 100% din interogări răspund `200` cu
  `mode: "keyword"`, iar acuratețea este identică cu a căutării clasice. Acest lucru este verificat și de
  `Given_OllamaUnavailable_When_EvaluatingSemanticSearch_Then_EveryQueryFallsBackAndScoresLikeKeywordSearch`.
- **`nomic-embed-text`** este antrenat cu prefixe de sarcină (`search_query:` / `search_document:`). Pipeline-ul
  actual nu le adaugă, deci rezultatele lui pot fi ușor subestimate față de o integrare dedicată. `bge-m3` nu
  are nevoie de prefixe și este multilingv, ceea ce contează pentru interogări în română.
- **Dimensiunea setului.** La 30 de interogări, o singură interogare înseamnă 3,3 puncte procentuale.
  Diferențe sub ~7 pp între modele nu sunt concludente.
- **Dashboard-ul din frontend și rate limiting-ul.** Ruta `/api/tutors/semantic-search` permite acum 15
  cereri/minut per IP. Dashboard-ul de evaluare trimite cele 30 de interogări secvențial, deci după a 15-a
  primește `429`, pe care îl raportează ca „error”. Benchmark-ul din backend nu este afectat, pentru că apelează
  handler-ul direct, fără HTTP.

## 7. Concluzii

1. Pipeline-ul vectorial (indexare `Name | Subjects | Bio` → cosinus → top 5) este corect cap-coadă: cu vectori
   pur lexicali atinge 80% Top-1 și 96,7% Top-3, față de 0% pentru căutarea clasică pe aceleași 30 de probleme.
2. Reziliența este verificată: la timeout (> 3,5 s) sau la eroare Ollama, răspunsul este `200` cu
   `mode: "keyword"`, fără erori 500.
3. Alegerea între `bge-m3` și `nomic-embed-text` rămâne deschisă până la rularea live (secțiunea 5). Criteriul
   recomandat este Top-3, apoi Top-1, cu condiția ca rata de fallback să rămână 0% la bugetul de 3,5 s.
   Un model care nu depășește proxy-ul lexical (96,7% Top-3) nu își justifică costul pe acest set.

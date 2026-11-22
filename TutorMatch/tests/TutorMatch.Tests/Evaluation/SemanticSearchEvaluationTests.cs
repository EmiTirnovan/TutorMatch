using System.Text.RegularExpressions;
using FluentAssertions;
using TutorMatch.Api.Features.SemanticSearch.Services;
using TutorMatch.Tests.Features.SemanticSearch.Fakes;
using Xunit;
using Xunit.Abstractions;

namespace TutorMatch.Tests.Evaluation;

/// <summary>
/// Set de evaluare (Ground Truth) si teste de masurare a acuratetei (Top-1 / Top-3, fallback rate, latenta)
/// pentru modulul de cautare semantica bazat pe IA. Ruleaza offline (fara Ollama); varianta live este in
/// <see cref="SemanticSearchLiveBenchmarkTests"/>.
/// </summary>
public class SemanticSearchEvaluationTests
{
    public const string LexicalProxyStrategy = "Lexical bag-of-words vectors (offline proxy)";
    public const string KeywordStrategy = "Classic keyword search (SearchTutors)";
    public const string OllamaDownStrategy = "Semantic search, Ollama down (fallback)";

    private readonly ITestOutputHelper _output;

    public SemanticSearchEvaluationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public record MockTutorProfile(Guid Id, string Name, string Bio, List<string> Subjects);

    public record EvaluationPair(
        string NaturalLanguageQuery,
        Guid ExpectedTutorId,
        string ExpectedTutorName,
        string DomainCategory);

    // Pool de tutori mock reprezentativi pentru diverse discipline si specializari
    public static readonly IReadOnlyList<MockTutorProfile> MockTutorPool = new List<MockTutorProfile>
    {
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Ion Popescu",
            "Profesor de matematica cu peste 10 ani de experienta. Predau analiza matematica, algebra liniara si geometrie.",
            new List<string> { "Matematica", "Analiza Matematica", "Algebra" }),

        new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Alexandru Radu",
            "Software engineer si trainer. Ajut studentii cu algoritmi, structuri de date avansate si programare orientata pe obiecte in C++ si Java.",
            new List<string> { "Informatica", "Algoritmi", "C++", "POO" }),

        new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Elena Dumitrescu",
            "Cercetator in fizica teoretica. Ofer meditatii la fizica clasica, mecanica cuantica si termodinamica pentru liceu si facultate.",
            new List<string> { "Fizica", "Mecanica", "Optica" }),

        new(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Maria Ionescu",
            "Profesoara certificata Cambridge CELTA. Pregatesc elevi si studenti pentru examenele IELTS, TOEFL si Cambridge C1/C2.",
            new List<string> { "Limba Engleza", "IELTS", "TOEFL" }),

        new(Guid.Parse("55555555-5555-5555-5555-555555555555"), "Andrei Vasilescu",
            "Medic rezident si pasionat de chimie organica. Pregatesc liceeni pentru admiterea la facultatile de medicina si farmacie.",
            new List<string> { "Chimie", "Chimie Organica", "Biologie" }),

        new(Guid.Parse("66666666-6666-6666-6666-666666666666"), "Cristian Stoica",
            "Full-stack developer specializat in React, TypeScript si Node.js. Explic concepte moderne de dezvoltare web si arhitecturi API.",
            new List<string> { "Web Development", "React", "TypeScript", "Node.js" }),

        new(Guid.Parse("77777777-7777-7777-7777-777777777777"), "Diana Munteanu",
            "AI researcher si data scientist. Meditez la Machine Learning, Deep Learning, procesare de limbaj natural si Python.",
            new List<string> { "Machine Learning", "Python", "Data Science", "AI" }),

        new(Guid.Parse("88888888-8888-8888-8888-888888888888"), "Mihai Enache",
            "Database administrator. Predau modelarea bazelor de date relationale, optimizare SQL, indexare si PostgreSQL.",
            new List<string> { "Baze de Date", "SQL", "PostgreSQL" }),

        new(Guid.Parse("99999999-9999-9999-9999-999999999999"), "Stefan Weber",
            "Vorbitor nativ si cadru didactic de limba germana. Cursuri interactive pentru nivelurile A1-C1 si pregatire Goethe Zertifikat.",
            new List<string> { "Limba Germana", "Goethe" }),

        new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Laura Gheorghe",
            "Economist si analist financiar. Explic concepte de microeconomie, macroeconomie si finante corporative.",
            new List<string> { "Economie", "Finante" }),

        new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Simona Stan",
            "Biolog si profesor. Pregatire pentru bacalaureat si olimpiade la anatomie, genetica si biologie celulara.",
            new List<string> { "Biologie", "Genetica", "Anatomie" })
    };

    // Setul de date de evaluare (30 de perechi intrebare in limbaj natural -> tutore asteptat).
    // Mirrored in frontend/src/app/core/data/evaluation-cases.ts (checked by a test below).
    public static readonly IReadOnlyList<EvaluationPair> GroundTruthEvaluationSet = new List<EvaluationPair>
    {
        // 1. Matematica
        new("Am nevoie de ajutor urgent cu analiza matematica, in special derivate si integrale duble",
            Guid.Parse("11111111-1111-1111-1111-111111111111"), "Ion Popescu", "Matematica"),
        new("Vreau sa inteleg algebra liniara, spatii vectoriale, vectori proprii si matrici",
            Guid.Parse("11111111-1111-1111-1111-111111111111"), "Ion Popescu", "Matematica"),
        new("Caut meditator pentru geometrie analitica si functii trigonometrice",
            Guid.Parse("11111111-1111-1111-1111-111111111111"), "Ion Popescu", "Matematica"),

        // 2. Algoritmi si Programare
        new("Vreau sa invat algoritmi si structuri de date pentru interviuri tehnice FAANG",
            Guid.Parse("22222222-2222-2222-2222-222222222222"), "Alexandru Radu", "Informatica"),
        new("Am dificultati la programare orientata pe obiecte in C++ si Java",
            Guid.Parse("22222222-2222-2222-2222-222222222222"), "Alexandru Radu", "Informatica"),
        new("Caut cineva sa-mi explice probleme de grafuri, Dijkstra si programare dinamica",
            Guid.Parse("22222222-2222-2222-2222-222222222222"), "Alexandru Radu", "Informatica"),
        new("Imi da segmentation fault cand lucrez cu pointeri si alocare dinamica in C++ si nu inteleg de ce",
            Guid.Parse("22222222-2222-2222-2222-222222222222"), "Alexandru Radu", "Informatica"),

        // 3. Fizica
        new("Caut profesor pentru fizica de liceu si facultate, capitolele de termodinamica si electromagnetism",
            Guid.Parse("33333333-3333-3333-3333-333333333333"), "Elena Dumitrescu", "Fizica"),
        new("Am nevoie de explicatii pentru ecuatia lui Schrodinger si mecanica cuantica",
            Guid.Parse("33333333-3333-3333-3333-333333333333"), "Elena Dumitrescu", "Fizica"),
        new("Ajutor la probleme de optica ondulatorie si mecanica fluidelor",
            Guid.Parse("33333333-3333-3333-3333-333333333333"), "Elena Dumitrescu", "Fizica"),

        // 4. Limba Engleza
        new("Pregatire intensiva si simulari pentru examenul Cambridge C1 Advanced",
            Guid.Parse("44444444-4444-4444-4444-444444444444"), "Maria Ionescu", "Limba Engleza"),
        new("Vreau sa iau un scor mare la IELTS Academic pentru a studia in strainatate",
            Guid.Parse("44444444-4444-4444-4444-444444444444"), "Maria Ionescu", "Limba Engleza"),
        new("Cursuri de conversatie fluenta in limba engleza pentru afaceri si redactare de eseuri",
            Guid.Parse("44444444-4444-4444-4444-444444444444"), "Maria Ionescu", "Limba Engleza"),

        // 5. Chimie
        new("Sinteze de chimie organica si mecanisme de reactie pentru admiterea la medicina UMF",
            Guid.Parse("55555555-5555-5555-5555-555555555555"), "Andrei Vasilescu", "Chimie"),
        new("Am nevoie de pregatire la chimie si compusi organici pentru examenul de admitere",
            Guid.Parse("55555555-5555-5555-5555-555555555555"), "Andrei Vasilescu", "Chimie"),
        new("Ma incurc la izomeri, alchene si reactii de substitutie la chimie organica inainte de simulare",
            Guid.Parse("55555555-5555-5555-5555-555555555555"), "Andrei Vasilescu", "Chimie"),

        // 6. Dezvoltare Web
        new("Cum sa construiesc aplicatii web moderne de la zero folosind React si TypeScript",
            Guid.Parse("66666666-6666-6666-6666-666666666666"), "Cristian Stoica", "Web Development"),
        new("Am nevoie de mentorat pentru crearea unui backend REST API cu Node.js si Express",
            Guid.Parse("66666666-6666-6666-6666-666666666666"), "Cristian Stoica", "Web Development"),
        new("Vreau sa inteleg frontend state management, hooks si optimizare de performanta",
            Guid.Parse("66666666-6666-6666-6666-666666666666"), "Cristian Stoica", "Web Development"),

        // 7. Inteligenta Artificiala si Machine Learning
        new("Cursuri practice de Machine Learning cu Python, regresie liniara, clasificare si retele neuronale",
            Guid.Parse("77777777-7777-7777-7777-777777777777"), "Diana Munteanu", "Machine Learning"),
        new("Vreau sa invat procesarea limbajului natural, tokenizare si modele transformer",
            Guid.Parse("77777777-7777-7777-7777-777777777777"), "Diana Munteanu", "Machine Learning"),
        new("Data science si analiza statistica folosind librariile pandas, numpy si matplotlib",
            Guid.Parse("77777777-7777-7777-7777-777777777777"), "Diana Munteanu", "Machine Learning"),

        // 8. Baze de Date
        new("Optimizare de query-uri SQL complexe, planuri de executie si indecsi in baze relationale",
            Guid.Parse("88888888-8888-8888-8888-888888888888"), "Mihai Enache", "Baze de Date"),
        new("Modelare de tabele, chei straine si normalizare a bazelor de date in PostgreSQL",
            Guid.Parse("88888888-8888-8888-8888-888888888888"), "Mihai Enache", "Baze de Date"),

        // 9. Limba Germana
        new("Vreau sa invat limba germana de la incepator pana la nivel mediu B2 pentru un job in Germania",
            Guid.Parse("99999999-9999-9999-9999-999999999999"), "Stefan Weber", "Limba Germana"),
        new("Pregatire specifica pentru promovarea certificatului Goethe Zertifikat B2",
            Guid.Parse("99999999-9999-9999-9999-999999999999"), "Stefan Weber", "Limba Germana"),

        // 10. Economie & Finante
        new("Explicatii la economie, cerere si oferta, politica monetara si finante corporative",
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Laura Gheorghe", "Economie"),
        new("Nu inteleg cum se calculeaza elasticitatea cererii si costul marginal pentru seminarul de microeconomie",
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Laura Gheorghe", "Economie"),

        // 11. Biologie
        new("Meditatii la biologie pentru bacalaureat: genetica moleculara, legile lui Mendel si anatomie umana",
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Simona Stan", "Biologie"),
        new("Am examen la biologie celulara si nu inteleg diferenta dintre mitoza si meioza",
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Simona Stan", "Biologie")
    };

    /// <summary>
    /// Calculeaza metrica Top-K Accuracy (procentul de intrebari pentru care tutorele asteptat se afla in primele K rezultate).
    /// </summary>
    public static double CalculateTopKAccuracy(
        IReadOnlyList<EvaluationPair> testCases,
        Func<string, IEnumerable<Guid>> ranker,
        int k = 3)
    {
        if (testCases is null || testCases.Count == 0 || ranker is null || k <= 0)
        {
            return 0.0;
        }

        int successfulMatches = 0;

        foreach (var testCase in testCases)
        {
            var topPredictions = ranker(testCase.NaturalLanguageQuery).Take(k).ToList();
            if (topPredictions.Contains(testCase.ExpectedTutorId))
            {
                successfulMatches++;
            }
        }

        return (double)successfulMatches / testCases.Count * 100.0;
    }

    [Fact]
    public void Given_GroundTruthEvaluationSet_When_InspectingSize_Then_ContainsBetween25And30TestCases()
    {
        GroundTruthEvaluationSet.Should().HaveCountGreaterOrEqualTo(25);
        GroundTruthEvaluationSet.Should().HaveCountLessOrEqualTo(30);
    }

    [Fact]
    public void Given_GroundTruthEvaluationSet_When_ValidatingQueries_Then_AllQueriesAreNonEmptyAndUnique()
    {
        GroundTruthEvaluationSet.Should().OnlyContain(item => !string.IsNullOrWhiteSpace(item.NaturalLanguageQuery));

        var queryList = GroundTruthEvaluationSet.Select(e => e.NaturalLanguageQuery.Trim().ToLowerInvariant()).ToList();
        queryList.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Given_GroundTruthEvaluationSet_When_CheckingTutors_Then_AllExpectedTutorsExistInMockPoolWithMatchingNames()
    {
        var poolNames = MockTutorPool.ToDictionary(t => t.Id, t => t.Name);

        GroundTruthEvaluationSet.Should().OnlyContain(item =>
            poolNames.ContainsKey(item.ExpectedTutorId) && poolNames[item.ExpectedTutorId] == item.ExpectedTutorName);
    }

    [Fact]
    public void Given_FrontendEvaluationCases_When_ComparedWithBackendGroundTruth_Then_QueriesAndExpectedTutorsMatch()
    {
        var frontendCases = ParseFrontendEvaluationCases(File.ReadAllText(RepositoryPaths.FrontendEvaluationCases));

        var backendCases = GroundTruthEvaluationSet
            .Select(e => (e.NaturalLanguageQuery, e.ExpectedTutorId.ToString(), e.ExpectedTutorName))
            .ToList();

        frontendCases.Should().Equal(backendCases);
    }

    [Fact]
    public void Given_PerfectRanker_When_CalculatingTop3Accuracy_Then_Returns100Percent()
    {
        Func<string, IEnumerable<Guid>> perfectRanker = query =>
        {
            var pair = GroundTruthEvaluationSet.First(e => e.NaturalLanguageQuery == query);
            return new[] { pair.ExpectedTutorId, Guid.NewGuid(), Guid.NewGuid() };
        };

        double accuracy = CalculateTopKAccuracy(GroundTruthEvaluationSet, perfectRanker, k: 3);

        accuracy.Should().Be(100.0);
    }

    [Fact]
    public void Given_UnsuccessfulRanker_When_CalculatingTop3Accuracy_Then_Returns0Percent()
    {
        Func<string, IEnumerable<Guid>> wrongRanker = _ => new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        double accuracy = CalculateTopKAccuracy(GroundTruthEvaluationSet, wrongRanker, k: 3);

        accuracy.Should().Be(0.0);
    }

    [Fact]
    public void Given_RankerWithTutorOnThirdPosition_When_CalculatingTop3Accuracy_Then_CountsAsSuccess()
    {
        Func<string, IEnumerable<Guid>> ranker = query =>
        {
            var pair = GroundTruthEvaluationSet.First(e => e.NaturalLanguageQuery == query);
            return new[] { Guid.NewGuid(), Guid.NewGuid(), pair.ExpectedTutorId, Guid.NewGuid() };
        };

        double accuracy = CalculateTopKAccuracy(GroundTruthEvaluationSet, ranker, k: 3);

        accuracy.Should().Be(100.0);
    }

    [Fact]
    public void Given_RankerWithTutorOnFourthPosition_When_CalculatingTop3Accuracy_Then_CountsAsMiss()
    {
        Func<string, IEnumerable<Guid>> ranker = query =>
        {
            var pair = GroundTruthEvaluationSet.First(e => e.NaturalLanguageQuery == query);
            return new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), pair.ExpectedTutorId };
        };

        double accuracy = CalculateTopKAccuracy(GroundTruthEvaluationSet, ranker, k: 3);

        accuracy.Should().Be(0.0);
    }

    [Fact]
    public void Given_MixedOutcomes_When_Summarizing_Then_ComputesTop1Top3FallbackRateAndMeanLatency()
    {
        var cases = GroundTruthEvaluationSet.Take(4).ToList();
        var outcomes = new List<EvaluationCaseOutcome>
        {
            new(cases[0], Rank: 1, IsFallback: false, LatencyMs: 10),
            new(cases[1], Rank: 3, IsFallback: false, LatencyMs: 20),
            new(cases[2], Rank: 4, IsFallback: false, LatencyMs: 30),
            new(cases[3], Rank: null, IsFallback: true, LatencyMs: 40)
        };

        var metrics = SemanticSearchEvaluator.Summarize("test", outcomes);

        metrics.Should().Be(new EvaluationMetrics("test", 4, Top1Accuracy: 25.0, Top3Accuracy: 50.0, FallbackRate: 25.0, MeanLatencyMs: 25.0));
    }

    [Fact]
    public void Given_NoOutcomes_When_Summarizing_Then_ReturnsZeroMetrics()
    {
        var metrics = SemanticSearchEvaluator.Summarize("empty", Array.Empty<EvaluationCaseOutcome>());

        metrics.Should().Be(new EvaluationMetrics("empty", 0, 0, 0, 0, 0));
    }

    [Fact]
    public async Task Given_RankerReturningExpectedTutorSecond_When_Running_Then_RecordsRankTwoForEveryCase()
    {
        Func<string, CancellationToken, Task<RankedSearchResult>> search = (query, _) =>
        {
            var pair = GroundTruthEvaluationSet.First(e => e.NaturalLanguageQuery == query);
            return Task.FromResult(new RankedSearchResult(new[] { Guid.NewGuid(), pair.ExpectedTutorId }, IsFallback: false));
        };

        var run = await SemanticSearchEvaluator.RunAsync("second", GroundTruthEvaluationSet, search);

        run.Outcomes.Should().OnlyContain(o => o.Rank == 2);
        run.Metrics.Top1Accuracy.Should().Be(0.0);
        run.Metrics.Top3Accuracy.Should().Be(100.0);
    }

    [Fact]
    public async Task Given_OllamaUnavailable_When_EvaluatingSemanticSearch_Then_EveryQueryFallsBackAndScoresLikeKeywordSearch()
    {
        var indexer = new FakeOllamaClient { EmbeddingGenerator = DeterministicTextEmbedder.Embed };
        await using var database = await EvaluationDatabase.CreateAsync(indexer);
        var downClient = new FakeOllamaClient { ExceptionToThrow = new HttpRequestException("Connection refused.") };

        var semantic = await SemanticSearchEvaluator.RunAsync(OllamaDownStrategy, GroundTruthEvaluationSet, database.SemanticStrategy(downClient, new OllamaOptions()));
        var keyword = await SemanticSearchEvaluator.RunAsync(KeywordStrategy, GroundTruthEvaluationSet, database.KeywordStrategy());

        semantic.Metrics.FallbackRate.Should().Be(100.0);
        semantic.Metrics.Top3Accuracy.Should().Be(keyword.Metrics.Top3Accuracy);
    }

    [Fact]
    public async Task Given_OfflineEvaluationDatabase_When_Indexing_Then_EveryMockTutorGetsAnEmbedding()
    {
        var indexer = new FakeOllamaClient { EmbeddingGenerator = DeterministicTextEmbedder.Embed };

        await using var database = await EvaluationDatabase.CreateAsync(indexer);

        (await database.CountIndexedTutorsAsync()).Should().Be(MockTutorPool.Count);
        indexer.CapturedPrompts.Should().Contain(
            "Alexandru Radu | Algoritmi, C++, Informatica, POO | Software engineer si trainer. Ajut studentii cu algoritmi, structuri de date avansate si programare orientata pe obiecte in C++ si Java.");
    }

    /// <summary>
    /// Deterministic comparison that runs in every `dotnet test`. The vector pipeline here uses lexical
    /// bag-of-words vectors instead of a real model, so it measures the pipeline and the dataset, not bge-m3
    /// or nomic-embed-text. The printed table is copied into docs/ai-evaluation-report.md.
    /// </summary>
    [Fact]
    public async Task Given_OfflineEmbedder_When_ComparingStrategies_Then_VectorPipelineServesAllQueriesAndBeatsKeywordSearch()
    {
        var embedder = new FakeOllamaClient { EmbeddingGenerator = DeterministicTextEmbedder.Embed };
        await using var database = await EvaluationDatabase.CreateAsync(embedder);
        var downClient = new FakeOllamaClient { ExceptionToThrow = new HttpRequestException("Connection refused.") };

        var lexical = await SemanticSearchEvaluator.RunAsync(LexicalProxyStrategy, GroundTruthEvaluationSet, database.SemanticStrategy(embedder, new OllamaOptions()));
        var keyword = await SemanticSearchEvaluator.RunAsync(KeywordStrategy, GroundTruthEvaluationSet, database.KeywordStrategy());
        var ollamaDown = await SemanticSearchEvaluator.RunAsync(OllamaDownStrategy, GroundTruthEvaluationSet, database.SemanticStrategy(downClient, new OllamaOptions()));
        _output.WriteLine(SemanticSearchEvaluator.ToMarkdownTable(new[] { lexical.Metrics, keyword.Metrics, ollamaDown.Metrics }));
        _output.WriteLine(SemanticSearchEvaluator.ToPerCaseMarkdownTable(new[] { lexical, keyword }));

        lexical.Metrics.FallbackRate.Should().Be(0.0);
        lexical.Metrics.Top3Accuracy.Should().BeGreaterThanOrEqualTo(lexical.Metrics.Top1Accuracy);
        lexical.Metrics.Top3Accuracy.Should().BeGreaterThan(keyword.Metrics.Top3Accuracy);
    }

    // Reads `evaluationCase('<key>', '<query>')` calls and resolves <key> through the TUTORS map.
    private static List<(string Query, string TutorId, string TutorName)> ParseFrontendEvaluationCases(string source)
    {
        var tutors = Regex.Matches(source, @"(\w+):\s*\{\s*id:\s*'([^']+)',\s*name:\s*'([^']+)'")
            .ToDictionary(m => m.Groups[1].Value, m => (Id: m.Groups[2].Value, Name: m.Groups[3].Value));

        return Regex.Matches(source, @"evaluationCase\('(\w+)',\s*'((?:[^'\\]|\\.)*)'\)")
            .Select(m => (m.Groups[2].Value.Replace("\\'", "'"), tutors[m.Groups[1].Value].Id, tutors[m.Groups[1].Value].Name))
            .ToList();
    }
}

using FluentAssertions;
using Xunit;

namespace TutorMatch.Tests.Evaluation;

/// <summary>
/// Set de evaluare (Ground Truth) si teste de masurare a acuratetei (Top-3 Accuracy)
/// pentru modulul de cautare semantica bazat pe IA.
/// </summary>
public class SemanticSearchEvaluationTests
{
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

    // Setul de date de evaluare (25 de perechi intrebare in limbaj natural -> tutore asteptat)
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

        // 11. Biologie
        new("Meditatii la biologie pentru bacalaureat: genetica moleculara, legile lui Mendel si anatomie umana",
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
    public void Given_GroundTruthEvaluationSet_When_InspectingSize_Then_ContainsBetween20And30TestCases()
    {
        // Assert
        GroundTruthEvaluationSet.Should().HaveCountGreaterOrEqualTo(20);
        GroundTruthEvaluationSet.Should().HaveCountLessOrEqualTo(30);
    }

    [Fact]
    public void Given_GroundTruthEvaluationSet_When_ValidatingQueries_Then_AllQueriesAreNonEmptyAndUnique()
    {
        // Assert
        GroundTruthEvaluationSet.Should().OnlyContain(item => !string.IsNullOrWhiteSpace(item.NaturalLanguageQuery));

        var queryList = GroundTruthEvaluationSet.Select(e => e.NaturalLanguageQuery.Trim().ToLowerInvariant()).ToList();
        queryList.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Given_GroundTruthEvaluationSet_When_CheckingTutors_Then_AllExpectedTutorsExistInMockPool()
    {
        // Arrange
        var poolTutorIds = MockTutorPool.Select(t => t.Id).ToHashSet();

        // Assert
        GroundTruthEvaluationSet.Should().OnlyContain(item => poolTutorIds.Contains(item.ExpectedTutorId));
    }

    [Fact]
    public void Given_PerfectRanker_When_CalculatingTop3Accuracy_Then_Returns100Percent()
    {
        // Arrange: ranker ce plaseaza intotdeauna tutorele corect pe prima pozitie
        Func<string, IEnumerable<Guid>> perfectRanker = query =>
        {
            var pair = GroundTruthEvaluationSet.First(e => e.NaturalLanguageQuery == query);
            return new[] { pair.ExpectedTutorId, Guid.NewGuid(), Guid.NewGuid() };
        };

        // Act
        double accuracy = CalculateTopKAccuracy(GroundTruthEvaluationSet, perfectRanker, k: 3);

        // Assert
        accuracy.Should().Be(100.0);
    }

    [Fact]
    public void Given_UnsuccessfulRanker_When_CalculatingTop3Accuracy_Then_Returns0Percent()
    {
        // Arrange: ranker ce nu returneaza niciodata tutorele asteptat
        Func<string, IEnumerable<Guid>> wrongRanker = _ => new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        // Act
        double accuracy = CalculateTopKAccuracy(GroundTruthEvaluationSet, wrongRanker, k: 3);

        // Assert
        accuracy.Should().Be(0.0);
    }

    [Fact]
    public void Given_RankerWithTutorOnThirdPosition_When_CalculatingTop3Accuracy_Then_CountsAsSuccess()
    {
        // Arrange: ranker ce pune tutorele asteptat pe pozitia 3 (index 2)
        Func<string, IEnumerable<Guid>> ranker = query =>
        {
            var pair = GroundTruthEvaluationSet.First(e => e.NaturalLanguageQuery == query);
            return new[] { Guid.NewGuid(), Guid.NewGuid(), pair.ExpectedTutorId, Guid.NewGuid() };
        };

        // Act
        double accuracy = CalculateTopKAccuracy(GroundTruthEvaluationSet, ranker, k: 3);

        // Assert
        accuracy.Should().Be(100.0);
    }

    [Fact]
    public void Given_RankerWithTutorOnFourthPosition_When_CalculatingTop3Accuracy_Then_CountsAsMiss()
    {
        // Arrange: tutorele este pe pozitia 4 (index 3), dincolo de top 3
        Func<string, IEnumerable<Guid>> ranker = query =>
        {
            var pair = GroundTruthEvaluationSet.First(e => e.NaturalLanguageQuery == query);
            return new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), pair.ExpectedTutorId };
        };

        // Act
        double accuracy = CalculateTopKAccuracy(GroundTruthEvaluationSet, ranker, k: 3);

        // Assert
        accuracy.Should().Be(0.0);
    }
}


using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Infrastructure.Persistence;
using TutorMatch.Api.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TutorMatch.Tests.Infrastructure.Persistence;

public class SubjectsCatalogPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TutorMatchDbContext _dbContext;
    private readonly SqliteTutorRepository _repository;

    public SubjectsCatalogPersistenceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TutorMatchDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TutorMatchDbContext(options);
        _dbContext.Database.EnsureCreated();

        _repository = new SqliteTutorRepository(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public void Given_NewSubject_When_GetOrCreateSubject_Then_CreatesNormalizedCatalogEntry()
    {
        var subject = _repository.GetOrCreateSubject("  Machine   Learning  ");

        subject.Should().NotBeNull();
        subject.Name.Should().Be("Machine Learning");
        subject.UsageCount.Should().Be(0);

        var count = _dbContext.Subjects.Count(s => s.Name == "Machine Learning");
        count.Should().Be(1);
    }

    [Fact]
    public void Given_ExistingSubjectWithDifferentCasing_When_GetOrCreateSubject_Then_ReusesExistingEntity()
    {
        var first = _repository.GetOrCreateSubject("Java");
        var second = _repository.GetOrCreateSubject("java");
        var third = _repository.GetOrCreateSubject("JAVA");

        first.Id.Should().Be(second.Id);
        second.Id.Should().Be(third.Id);

        _dbContext.Subjects.Count().Should().Be(1);
    }

    [Fact]
    public void Given_NewTutorWithSubjects_When_AddingTutor_Then_IncrementsUsageCountForAssociatedSubjects()
    {
        var subj1 = _repository.GetOrCreateSubject("Algorithms");
        var subj2 = _repository.GetOrCreateSubject("Data Structures");

        var tutor = new Tutor("Tutor 1", "t1@test.com", "Bio 1", 100m);
        tutor.AddSubject(subj1);
        tutor.AddSubject(subj2);

        _repository.Add(tutor);

        var reloadedSubj1 = _dbContext.Subjects.Find(subj1.Id)!;
        var reloadedSubj2 = _dbContext.Subjects.Find(subj2.Id)!;

        reloadedSubj1.UsageCount.Should().Be(1);
        reloadedSubj2.UsageCount.Should().Be(1);
    }

    [Fact]
    public void Given_UpdatedProfile_When_RetainingRemovingAndAddingSubjects_Then_MaintainsConsistentUsageCounts()
    {
        var java = _repository.GetOrCreateSubject("Java");
        var python = _repository.GetOrCreateSubject("Python");
        var csharp = _repository.GetOrCreateSubject("C#");

        var tutor = new Tutor("Tutor Polyglot", "poly@test.com", "Polyglot Bio", 120m);
        tutor.AddSubject(java);
        tutor.AddSubject(python);
        _repository.Add(tutor);

        _dbContext.Subjects.Find(java.Id)!.UsageCount.Should().Be(1);
        _dbContext.Subjects.Find(python.Id)!.UsageCount.Should().Be(1);
        _dbContext.Subjects.Find(csharp.Id)!.UsageCount.Should().Be(0);

        // Update profile: Keep Java (retained), Remove Python, Add C#
        var updatedTutor = new Tutor("Tutor Polyglot", "poly@test.com", "Updated Bio", 130m, id: tutor.Id);
        updatedTutor.AddSubject(java);
        updatedTutor.AddSubject(csharp);

        _repository.Update(updatedTutor);

        var reloadedJava = _dbContext.Subjects.Find(java.Id)!;
        var reloadedPython = _dbContext.Subjects.Find(python.Id)!;
        var reloadedCSharp = _dbContext.Subjects.Find(csharp.Id)!;

        reloadedJava.UsageCount.Should().Be(1);    // Retained -> unchanged
        reloadedPython.UsageCount.Should().Be(0);  // Removed -> decremented to 0
        reloadedCSharp.UsageCount.Should().Be(1);  // Added -> incremented to 1
    }

    [Fact]
    public void Given_DeletedTutor_When_Deleting_Then_DecrementsAssociatedSubjectUsageCounts()
    {
        var subj = _repository.GetOrCreateSubject("Operating Systems");
        var tutor = new Tutor("OS Tutor", "os@test.com", "OS Bio", 90m);
        tutor.AddSubject(subj);
        _repository.Add(tutor);

        _dbContext.Subjects.Find(subj.Id)!.UsageCount.Should().Be(1);

        var deleted = _repository.Delete(tutor.Id);
        deleted.Should().BeTrue();

        _dbContext.Subjects.Find(subj.Id)!.UsageCount.Should().Be(0);
    }

    [Fact]
    public void Given_DuplicateSubjectNameCaseInsensitive_When_InsertingDirectly_Then_ThrowsUniqueConstraintViolation()
    {
        _dbContext.Subjects.Add(new Subject("Database"));
        _dbContext.SaveChanges();

        _dbContext.Subjects.Add(new Subject("database"));
        var action = () => _dbContext.SaveChanges();

        action.Should().Throw<DbUpdateException>();
    }
}

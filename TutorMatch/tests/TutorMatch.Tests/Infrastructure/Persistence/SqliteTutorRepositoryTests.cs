using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Infrastructure.Persistence;
using TutorMatch.Api.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TutorMatch.Tests.Infrastructure.Persistence;

public class SqliteTutorRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TutorMatchDbContext _context;
    private readonly SqliteTutorRepository _repository;

    public SqliteTutorRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TutorMatchDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TutorMatchDbContext(options);
        _context.Database.EnsureCreated();

        _repository = new SqliteTutorRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private static Tutor CreateSampleTutor(string name = "Emi Tirnovan", Guid? id = null)
    {
        return new Tutor(
            name: name,
            email: "emi@example.com",
            bio: "Experienced tutor in Algorithms and OOP.",
            hourlyRate: 80m,
            id: id ?? Guid.NewGuid());
    }

    [Fact]
    public void Add_ValidTutor_ShouldStoreSuccessfully()
    {
        var tutor = CreateSampleTutor();

        _repository.Add(tutor);
        var retrieved = _repository.GetById(tutor.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(tutor.Id);
        retrieved.Name.Should().Be(tutor.Name);
    }

    [Fact]
    public void Add_NullTutor_ShouldThrowArgumentNullException()
    {
        var act = () => _repository.Add(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Add_DuplicateId_ShouldThrowInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var tutor1 = CreateSampleTutor("Tutor 1", id);
        var tutor2 = CreateSampleTutor("Tutor 2", id);

        _repository.Add(tutor1);

        var act = () => _repository.Add(tutor2);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{id}*");
    }

    [Fact]
    public void GetById_WhenNotFound_ShouldReturnNullGracefully()
    {
        var result = _repository.GetById(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public void GetAll_ShouldReturnAllTutors()
    {
        var tutor1 = CreateSampleTutor("Tutor 1");
        var tutor2 = CreateSampleTutor("Tutor 2");

        _repository.Add(tutor1);
        _repository.Add(tutor2);

        var all = _repository.GetAll();

        all.Should().HaveCount(2);
        all.Should().Contain(t => t.Id == tutor1.Id);
        all.Should().Contain(t => t.Id == tutor2.Id);
    }

    [Fact]
    public void GetAll_WhenEmpty_ShouldReturnEmptyList()
    {
        var all = _repository.GetAll();
        all.Should().BeEmpty();
    }

    [Fact]
    public void Update_ExistingTutor_ShouldUpdateSuccessfully()
    {
        var tutor = CreateSampleTutor();
        _repository.Add(tutor);

        tutor.UpdateProfile("New Name", "New Bio", 100m);
        _repository.Update(tutor);

        var retrieved = _repository.GetById(tutor.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("New Name");
        retrieved.Bio.Should().Be("New Bio");
        retrieved.HourlyRate.Should().Be(100m);
    }

    [Fact]
    public void Update_NonExistentTutor_ShouldThrowKeyNotFoundException()
    {
        var tutor = CreateSampleTutor();

        var act = () => _repository.Update(tutor);
        act.Should().Throw<KeyNotFoundException>()
            .WithMessage($"*{tutor.Id}*");
    }

    [Fact]
    public void Delete_ExistingTutor_ShouldReturnTrueAndRemove()
    {
        var tutor = CreateSampleTutor();
        _repository.Add(tutor);

        var result = _repository.Delete(tutor.Id);

        result.Should().BeTrue();
        _repository.GetById(tutor.Id).Should().BeNull();
    }

    [Fact]
    public void Delete_NonExistentTutor_ShouldReturnFalse()
    {
        var result = _repository.Delete(Guid.NewGuid());
        result.Should().BeFalse();
    }

    [Fact]
    public void Availability_ValueObjects_ShouldPersistAndRetrieveCorrectly()
    {
        var tutor = CreateSampleTutor();
        var now = DateTime.UtcNow.Date.AddDays(1);
        var slot = new DateRange(now.AddHours(9), now.AddHours(12));
        tutor.AddAvailability(slot);

        _repository.Add(tutor);

        var retrieved = _repository.GetById(tutor.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Availability.Should().HaveCount(1);
        retrieved.Availability[0].Start.Should().Be(slot.Start);
        retrieved.Availability[0].End.Should().Be(slot.End);
    }
}

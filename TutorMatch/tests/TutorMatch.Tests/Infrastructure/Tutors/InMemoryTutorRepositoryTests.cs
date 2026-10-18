namespace TutorMatch.Tests.Infrastructure.Tutors;

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Infrastructure;
using TutorMatch.Api.Infrastructure.Tutors;
using Xunit;

public class InMemoryTutorRepositoryTests
{
    private readonly InMemoryTutorRepository _repository = new();

    private static Tutor CreateSampleTutor(string name = "Emi Tirnovan", Guid? id = null)
    {
        return new Tutor(
            name: name,
            email: "emi@example.com",
            bio: "Student pasionat de algoritmi",
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
        act.Should().Throw<InvalidOperationException>().WithMessage("*exista deja*");
    }

    [Fact]
    public void GetById_WhenNotFound_ShouldReturnNullGracefully()
    {
        var result = _repository.GetById(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public void GetAll_ShouldReturnSnapshotOfAllTutors()
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

        tutor.UpdateProfile("Nume Nou", "Bio Nou", 100m);
        _repository.Update(tutor);

        var retrieved = _repository.GetById(tutor.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Nume Nou");
        retrieved.HourlyRate.Should().Be(100m);
    }

    [Fact]
    public void Update_NonExistentTutor_ShouldThrowKeyNotFoundException()
    {
        var tutor = CreateSampleTutor();

        var act = () => _repository.Update(tutor);
        act.Should().Throw<KeyNotFoundException>().WithMessage("*nu a fost gasit*");
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
    public void ConcurrentAdds_ShouldBeThreadSafe()
    {
        const int count = 100;
        var tutors = Enumerable.Range(0, count)
            .Select(i => CreateSampleTutor($"Tutor {i}"))
            .ToList();

        Parallel.ForEach(tutors, tutor =>
        {
            _repository.Add(tutor);
        });

        _repository.GetAll().Should().HaveCount(count);
    }

    [Fact]
    public void DependencyInjection_ShouldRegisterAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddTutorInfrastructure();

        var provider = services.BuildServiceProvider();
        var instance1 = provider.GetRequiredService<ITutorRepository>();
        var instance2 = provider.GetRequiredService<ITutorRepository>();

        instance1.Should().NotBeNull();
        instance1.Should().BeOfType<InMemoryTutorRepository>();
        instance1.Should().BeSameAs(instance2); // Singleton
    }
}

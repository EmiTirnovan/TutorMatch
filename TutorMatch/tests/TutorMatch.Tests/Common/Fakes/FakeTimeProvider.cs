namespace Microsoft.Extensions.Time.Testing;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FakeTimeProvider() : this(DateTimeOffset.UtcNow)
    {
    }

    public FakeTimeProvider(DateTimeOffset startDateTime)
    {
        _utcNow = startDateTime;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void SetUtcNow(DateTimeOffset value) => _utcNow = value;

    public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);
}


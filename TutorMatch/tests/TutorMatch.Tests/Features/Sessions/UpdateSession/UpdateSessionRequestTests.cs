using FluentAssertions;
using TutorMatch.Api.Features.Sessions.UpdateSession;
using Xunit;

namespace TutorMatch.Tests.Features.Sessions.UpdateSession;

public class UpdateSessionRequestTests
{
    [Fact]
    public void Given_validDates_When_creatingRequest_Then_keepsInputValues()
    {
        var startTime = new DateTime(2026, 10, 20, 10, 0, 0);
        var endTime = new DateTime(2026, 10, 20, 11, 0, 0);

        var request = new UpdateSessionRequest(startTime, endTime);

        request.StartTime.Should().Be(startTime);
        request.EndTime.Should().Be(endTime);
        request.Should().Be(new UpdateSessionRequest(startTime, endTime));
    }
}

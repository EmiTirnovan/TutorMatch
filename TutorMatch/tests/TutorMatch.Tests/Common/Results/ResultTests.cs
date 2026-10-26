using FluentAssertions;
using Microsoft.AspNetCore.Http;
using TutorMatch.Api.Common.Results;
using Xunit;

namespace TutorMatch.Tests.Common.Results;

public class ResultTests
{
    private sealed class TestableResult : Result
    {
        public TestableResult(bool isSuccess, Error error)
            : base(isSuccess, error)
        {
        }
    }

    private sealed class TestableGenericResult<TValue> : Result<TValue>
    {
        public TestableGenericResult(TValue value, bool isSuccess, Error error)
            : base(value, isSuccess, error)
        {
        }
    }

    [Fact]
    public void Given_successfulResult_When_matching_Then_returnsSuccessBranch()
    {
        var result = Result.Success();

        var output = result.Match(
            () => "success",
            error => error.Code);

        output.Should().Be("success");
    }

    [Fact]
    public void Given_failedResult_When_matching_Then_returnsFailureBranch()
    {
        var error = Error.Validation("validation", "invalid");
        var result = Result.Failure(error);

        var output = result.Match(
            () => "success",
            failure => failure.Code);

        output.Should().Be("validation");
    }

    [Fact]
    public void Given_successFactory_When_creatingResult_Then_resultIsSuccessfulAndHasNoError()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Given_failureFactory_When_creatingFailureResult_Then_resultIsFailureAndHasError()
    {
        var error = Error.NotFound("missing", "not found");

        var result = Result.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Given_successResultWithNoError_When_creatingFailureResult_Then_throwsInvalidOperationException()
    {
        var action = () => Result.Failure(Error.None);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Given_resultConstructor_When_givenInvalidStates_Then_throwsInvalidOperationException()
    {
        var action1 = () => new TestableResult(true, Error.Validation("x", "y"));
        var action2 = () => new TestableResult(false, Error.None);

        action1.Should().Throw<InvalidOperationException>();
        action2.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Given_genericResultConstructor_When_givenInvalidStates_Then_throwsInvalidOperationException()
    {
        var action1 = () => new TestableGenericResult<int>(42, true, Error.Validation("x", "y"));
        var action2 = () => new TestableGenericResult<int>(42, false, Error.None);

        action1.Should().Throw<InvalidOperationException>();
        action2.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Given_successResult_When_creatingGenericSuccessfulResult_Then_returnsValue()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Given_failureResult_When_creatingGenericFailureResult_Then_resultIsFailureAndValueThrows()
    {
        var error = Error.Conflict("conflict", "already exists");
        var result = Result<int>.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
        var action = () => _ = result.Value;
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Given_genericResult_When_matching_Then_returnsCorrectBranch()
    {
        var result = Result<string>.Success("ok");

        var output = result.Match(
            value => value.ToUpperInvariant(),
            error => error.Description);

        output.Should().Be("OK");
    }

    [Fact]
    public void Given_failureGenericResult_When_matching_Then_returnsFailureBranch()
    {
        var error = Error.Failure("failure", "bad");
        var result = Result<string>.Failure(error);

        var output = result.Match(
            value => value,
            failure => failure.Code);

        output.Should().Be("failure");
    }

    [Fact]
    public void Given_implicitConversion_When_convertingValueToResult_Then_returnsSuccessfulResult()
    {
        Result<int> result = 10;

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(10);
    }

    [Fact]
    public void Given_notFoundFactory_When_creatingError_Then_returnExpectedErrorProperties()
    {
        var error = Error.NotFound("not-found", "missing");

        error.Code.Should().Be("not-found");
        error.Description.Should().Be("missing");
        error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void Given_validationFactory_When_creatingError_Then_returnExpectedErrorProperties()
    {
        var error = Error.Validation("validation", "invalid");

        error.Code.Should().Be("validation");
        error.Description.Should().Be("invalid");
        error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Given_conflictFactory_When_creatingError_Then_returnExpectedErrorProperties()
    {
        var error = Error.Conflict("conflict", "duplicate");

        error.Code.Should().Be("conflict");
        error.Description.Should().Be("duplicate");
        error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void Given_failureFactory_When_creatingError_Then_returnExpectedErrorProperties()
    {
        var error = Error.Failure("failure", "unexpected");

        error.Code.Should().Be("failure");
        error.Description.Should().Be("unexpected");
        error.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void Given_errorNone_When_accessingProperties_Then_valuesAreEmptyAndFailureType()
    {
        Error.None.Code.Should().BeEmpty();
        Error.None.Description.Should().BeEmpty();
        Error.None.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void Given_successfulResult_When_convertingToHttpResult_Then_returnsNoContent()
    {
        var result = Result.Success();

        var httpResult = result.ToHttpResult();

        httpResult.Should().NotBeNull();
    }

    [Fact]
    public void Given_notFoundFailure_When_convertingToHttpResult_Then_returnsNotFoundResult()
    {
        var result = Result.Failure(Error.NotFound("missing", "not found"));

        var httpResult = result.ToHttpResult();

        httpResult.Should().NotBeNull();
    }

    [Fact]
    public void Given_conflictFailure_When_convertingToHttpResult_Then_returnsConflictResult()
    {
        var result = Result.Failure(Error.Conflict("duplicate", "already exists"));

        var httpResult = result.ToHttpResult();

        httpResult.Should().NotBeNull();
    }

    [Fact]
    public void Given_validationFailure_When_convertingToHttpResult_Then_returnsBadRequestResult()
    {
        var result = Result.Failure(Error.Validation("validation", "invalid"));

        var httpResult = result.ToHttpResult();

        httpResult.Should().NotBeNull();
    }

    [Fact]
    public void Given_successfulGenericResult_When_convertingToHttpResult_Then_returnsOkResult()
    {
        var result = Result<int>.Success(42);

        var httpResult = result.ToHttpResult(value => TypedResults.Ok(value));

        httpResult.Should().NotBeNull();
    }

    [Fact]
    public void Given_successfulGenericResultWithoutCallback_When_convertingToHttpResult_Then_returnsOkResult()
    {
        var result = Result<int>.Success(42);

        var httpResult = result.ToHttpResult();

        httpResult.Should().NotBeNull();
    }

    [Fact]
    public void Given_failureGenericResult_When_convertingToHttpResult_Then_returnsErrorResult()
    {
        var result = Result<int>.Failure(Error.NotFound("missing", "not found"));

        var httpResult = result.ToHttpResult();

        httpResult.Should().NotBeNull();
    }
}

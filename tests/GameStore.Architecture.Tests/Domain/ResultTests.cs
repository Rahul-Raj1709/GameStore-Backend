using FluentAssertions;
using GameStore.Domain.Shared;
using Xunit;

namespace GameStore.Application.Tests.Domain;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateValidResult_WithValue()
    {
        // Arrange
        var expectedValue = "Test Data";

        // Act
        // Calls the static generic method in your Result.cs
        var result = Result.Success(expectedValue);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(expectedValue);
    }

    [Fact]
    public void Failure_ShouldCreateInvalidResult_WithError()
    {
        // Arrange
        var error = new Error("Record.NotFound", "The record was not found.");

        // Act
        // Calling Result.Failure<string> ensures we get back a Result<string>, not the base Result.
        var result = Result.Failure<string>(error);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);

        // Accessing Value on a failed result should throw the InvalidOperationException
        // as explicitly designed in your Result<TValue> class.
        Action action = () => { var val = result.Value; };
        action.Should().Throw<InvalidOperationException>();
    }
}
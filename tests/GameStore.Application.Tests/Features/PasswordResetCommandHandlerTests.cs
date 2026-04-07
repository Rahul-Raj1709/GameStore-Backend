using FluentAssertions;
using GameStore.Application.Features.Auth.Commands;
using GameStore.Application.Interfaces.Security;
using GameStore.Domain.Shared;
using NSubstitute;
using Xunit;

namespace GameStore.Application.Tests.Features;

public class PasswordResetCommandHandlerTests
{
    private readonly IIdentityService _mockIdentityService;

    public PasswordResetCommandHandlerTests()
    {
        _mockIdentityService = Substitute.For<IIdentityService>();
    }

    [Fact]
    public async Task RequestPasswordReset_ShouldReturnToken_FromIdentityService()
    {
        // Arrange
        var handler = new RequestPasswordResetCommandHandler(_mockIdentityService);
        var command = new RequestPasswordResetCommand("test@test.com");
        var expectedToken = "secure-reset-token-123";

        _mockIdentityService.GeneratePasswordResetTokenAsync(command.Email)
            .Returns(Result.Success(expectedToken));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedToken);
        await _mockIdentityService.Received(1).GeneratePasswordResetTokenAsync(command.Email);
    }

    [Fact]
    public async Task ResetPassword_ShouldCallIdentityService_AndReturnResult()
    {
        // Arrange
        var handler = new ResetPasswordCommandHandler(_mockIdentityService);
        var command = new ResetPasswordCommand("test@test.com", "token-123", "NewPassword123!");

        _mockIdentityService.ResetPasswordAsync(command.Email, command.Token, command.NewPassword)
            .Returns(Result.Success());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _mockIdentityService.Received(1).ResetPasswordAsync(command.Email, command.Token, command.NewPassword);
    }
}
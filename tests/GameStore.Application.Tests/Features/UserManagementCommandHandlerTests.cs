using FluentAssertions;
using GameStore.Application.Features.Users.Commands;
using GameStore.Application.Interfaces;
using GameStore.Application.Interfaces.Security;
using GameStore.Domain.Constants;
using GameStore.Domain.Entities;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace GameStore.Application.Tests.Features;

public class UserManagementCommandHandlerTests
{
    private readonly IApplicationDbContext _mockDbContext;
    private readonly IIdentityService _mockIdentityService;

    public UserManagementCommandHandlerTests()
    {
        _mockDbContext = Substitute.For<IApplicationDbContext>();
        _mockIdentityService = Substitute.For<IIdentityService>();
    }

    [Fact]
    public async Task UpdateUserStatus_ShouldToggleIsActive_AndSave()
    {
        // Arrange
        var handler = new UpdateUserStatusCommandHandler(_mockDbContext);
        var user = new User { Id = 1, IsActive = false, Username = "TestAdmin", Email = "test@test.com" };
        var command = new UpdateUserStatusCommand(1, true);

        var mockDbSet = Substitute.For<DbSet<User>, IQueryable<User>>();
        mockDbSet.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<User?>(user));

        _mockDbContext.Users.Returns(mockDbSet);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeTrue("The handler should have updated the IsActive flag to true.");
        await _mockDbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveUser_ShouldDeleteFromBothIdentityAndAppDb()
    {
        // Arrange
        var handler = new RemoveUserCommandHandler(_mockDbContext, _mockIdentityService);
        var user = new User { Id = 1, IdentityId = "identity-123", Username = "TestUser", Email = "test@test.com", Role = RoleConstants.Admin };
        var command = new RemoveUserCommand(1, 999); // Passed CurrentUserId = 999 to simulate someone else doing the deletion

        var mockDbSet = Substitute.For<DbSet<User>, IQueryable<User>>();
        mockDbSet.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<User?>(user));

        _mockDbContext.Users.Returns(mockDbSet);
        _mockIdentityService.DeleteUserAsync(user.IdentityId).Returns(Result.Success());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _mockIdentityService.Received(1).DeleteUserAsync("identity-123");
        mockDbSet.Received(1).Remove(user);
        await _mockDbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveUser_ShouldFail_WhenDeletingSelf()
    {
        // Arrange
        var handler = new RemoveUserCommandHandler(_mockDbContext, _mockIdentityService);
        var command = new RemoveUserCommand(1, 1); // UserId = 1, CurrentUserId = 1

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.Forbidden");
    }
}
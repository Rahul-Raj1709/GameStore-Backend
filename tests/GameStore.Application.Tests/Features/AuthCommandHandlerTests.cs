using FluentAssertions;
using GameStore.Application.DTOs.Auth;
using GameStore.Application.Features.Auth.Commands;
using GameStore.Application.Features.Auth.Queries;
using GameStore.Application.Interfaces;
using GameStore.Application.Interfaces.Security;
using GameStore.Domain.Constants;
using GameStore.Domain.Entities;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;
using MockQueryable.NSubstitute;

namespace GameStore.Application.Tests.Features;

public class AuthCommandHandlerTests
{
    private readonly IApplicationDbContext _mockDbContext;
    private readonly IIdentityService _mockIdentityService;
    private readonly IJwtTokenGenerator _mockJwtGenerator;

    public AuthCommandHandlerTests()
    {
        _mockDbContext = Substitute.For<IApplicationDbContext>();
        _mockIdentityService = Substitute.For<IIdentityService>();
        _mockJwtGenerator = Substitute.For<IJwtTokenGenerator>();
    }

    [Fact]
    public async Task Register_ShouldSetIsActiveFalse_WhenRoleIsAdmin()
    {
        // Arrange
        var handler = new RegisterCommandHandler(_mockDbContext, _mockIdentityService, _mockJwtGenerator);
        var command = new RegisterCommand("AdminUser", "admin@test.com", "Password123!", "Test Admin", RoleConstants.Admin);

        _mockIdentityService.RegisterUserAsync(command.Email, command.Username, command.Password, command.Role!)
            .Returns(Result.Success(("identity-1", "refresh-token")));

        _mockDbContext.Users.Returns(Substitute.For<DbSet<User>>());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("PENDING_ACTIVATION");

        // Ensure the entity saved to DB had IsActive = false
        _mockDbContext.Users.Received(1).Add(Arg.Is<User>(u => u.IsActive == false && u.Role == RoleConstants.Admin));
        await _mockDbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_ShouldReturnFailure_WhenUserIsInactive()
    {
        // Arrange
        var handler = new LoginQueryHandler(_mockDbContext, _mockIdentityService, _mockJwtGenerator);
        var query = new LoginQuery("admin@test.com", "Password123!");

        _mockIdentityService.AuthenticateUserAsync(query.Email, query.Password)
            .Returns(Result.Success(("identity-1", "refresh-token")));

        // Mock the user as Inactive
        var inactiveUser = new User { Id = 1, IdentityId = "identity-1", IsActive = false, Email = query.Email };

        // Mocking IQueryable for FirstOrDefaultAsync
        var usersList = new List<User> { inactiveUser }.AsQueryable();
        var mockDbSet = usersList.BuildMockDbSet();

        _mockDbContext.Users.Returns(mockDbSet);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.Inactive");
        _mockJwtGenerator.DidNotReceive().GenerateToken(Arg.Any<User>());
    }
}
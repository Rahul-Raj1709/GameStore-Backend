using FluentAssertions;
using GameStore.Application.Features.Games.Commands;
using GameStore.Application.Interfaces;
using GameStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using Xunit;

namespace GameStore.Application.Tests.Features;

public class UpdateGameCommandHandlerTests
{
    private readonly IApplicationDbContext _mockDbContext;
    private readonly HybridCache _mockCache;
    private readonly UpdateGameCommandHandler _handler;

    public UpdateGameCommandHandlerTests()
    {
        _mockDbContext = Substitute.For<IApplicationDbContext>();
        _mockCache = Substitute.For<HybridCache>(); // Mock the cache
        _handler = new UpdateGameCommandHandler(_mockDbContext, _mockCache); // Pass the cache
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenUserIsOwner()
    {
        // Arrange
        int gameId = 1;
        int ownerId = 100;
        var game = new Game { Id = gameId, Name = "Old Name", OwnerId = ownerId };

        var command = new UpdateGameCommand(gameId, "New Name", "Desc", null, 1, 50m, new DateOnly(2025, 1, 1), ownerId, IsSuperAdmin: false);

        var mockDbSet = Substitute.For<DbSet<Game>, IQueryable<Game>>();
        mockDbSet.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<Game?>(game));

        _mockDbContext.Games.Returns(mockDbSet);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        game.Name.Should().Be("New Name");
        await _mockDbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenUserIsNotOwnerButIsSuperAdmin()
    {
        // Arrange
        int gameId = 1;
        int actualOwnerId = 100;
        int superAdminId = 999;

        var game = new Game { Id = gameId, Name = "Old Name", OwnerId = actualOwnerId };
        var command = new UpdateGameCommand(gameId, "New Name", "Desc", null, 1, 50m, new DateOnly(2025, 1, 1), superAdminId, IsSuperAdmin: true);

        var mockDbSet = Substitute.For<DbSet<Game>, IQueryable<Game>>();
        mockDbSet.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<Game?>(game));

        _mockDbContext.Games.Returns(mockDbSet);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _mockDbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotOwnerAndNotSuperAdmin()
    {
        // Arrange
        int gameId = 1;
        int actualOwnerId = 100;
        int maliciousUserId = 500;

        var game = new Game { Id = gameId, Name = "Old Name", OwnerId = actualOwnerId };
        var command = new UpdateGameCommand(gameId, "New Name", "Desc", null, 1, 50m, new DateOnly(2025, 1, 1), maliciousUserId, IsSuperAdmin: false);

        var mockDbSet = Substitute.For<DbSet<Game>, IQueryable<Game>>();
        mockDbSet.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<Game?>(game));

        _mockDbContext.Games.Returns(mockDbSet);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.Forbidden");
        await _mockDbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
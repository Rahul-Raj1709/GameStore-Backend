using FluentAssertions;
using GameStore.Application.Features.Games.Commands;
using GameStore.Application.Interfaces;
using GameStore.Domain.Entities;
using Microsoft.Extensions.Caching.Hybrid;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace GameStore.Application.Tests.Features;

public class ReviewCommandHandlerTests
{
    private readonly IApplicationDbContext _mockDbContext;
    private readonly HybridCache _mockCache;

    public ReviewCommandHandlerTests()
    {
        _mockDbContext = Substitute.For<IApplicationDbContext>();
        _mockCache = Substitute.For<HybridCache>(); // Mocking the newly injected cache
    }

    [Fact]
    public async Task AddReview_ShouldCreateReview_AndRecalculateRating()
    {
        // Arrange
        var handler = new AddReviewCommandHandler(_mockDbContext, _mockCache);
        var command = new AddReviewCommand(GameId: 1, UserId: 100, Rating: 4, Comment: "Great game!");

        var game = new Game { Id = 1, Name = "Test Game", AverageRating = 0 };

        // Mock Games DbSet
        var gamesList = new List<Game> { game };
        var mockGames = gamesList.BuildMockDbSet();
        mockGames.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>()).Returns(new ValueTask<Game?>(game));
        _mockDbContext.Games.Returns(mockGames);

        // Mock Reviews DbSet (Empty initially)
        var reviewsList = new List<Review>();
        var mockReviews = reviewsList.BuildMockDbSet();
        _mockDbContext.Reviews.Returns(mockReviews);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockReviews.Received(1).Add(Arg.Is<Review>(r => r.GameId == 1 && r.Rating == 4));

        // SaveChangesAsync is called twice: once for the Add, once for the Recalculation
        await _mockDbContext.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddReview_ShouldFail_WhenRatingIsOutOfBounds()
    {
        // Arrange
        var handler = new AddReviewCommandHandler(_mockDbContext, _mockCache);
        var command = new AddReviewCommand(GameId: 1, UserId: 100, Rating: 6, Comment: "Too high!"); // Invalid rating

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Review.InvalidRating");
        await _mockDbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateReview_ShouldFail_WhenUserIsNotOwnerAndNotSuperAdmin()
    {
        // Arrange
        var handler = new UpdateReviewCommandHandler(_mockDbContext, _mockCache);

        // Added IsAdmin: false
        var command = new UpdateReviewCommand(ReviewId: 1, UserId: 999, Rating: 5, Comment: "Hacked", IsSuperAdmin: false, IsAdmin: false);

        var review = new Review { Id = 1, UserId = 100, GameId = 1, Game = new Game() }; // Owned by User 100

        var reviewsList = new List<Review> { review };
        var mockReviews = reviewsList.BuildMockDbSet();
        _mockDbContext.Reviews.Returns(mockReviews);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.Forbidden");
    }

    [Fact]
    public async Task DeleteReview_ShouldRemoveReview_WhenUserIsSuperAdmin()
    {
        // Arrange
        var handler = new DeleteReviewCommandHandler(_mockDbContext, _mockCache);

        // Added IsAdmin: false
        var command = new DeleteReviewCommand(ReviewId: 1, UserId: 999, IsSuperAdmin: true, IsAdmin: false); // SuperAdmin deleting

        var review = new Review { Id = 1, UserId = 100, GameId = 1, Game = new Game() }; // Owned by User 100

        var reviewsList = new List<Review> { review };
        var mockReviews = reviewsList.BuildMockDbSet();
        _mockDbContext.Reviews.Returns(mockReviews);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockReviews.Received(1).Remove(review);
    }

    // --- NEW TESTS FOR ADMIN (GAME OWNER) LOGIC ---

    [Fact]
    public async Task DeleteReview_ShouldRemoveReview_WhenUserIsAdminAndOwnsGame()
    {
        // Arrange
        var handler = new DeleteReviewCommandHandler(_mockDbContext, _mockCache);

        // Admin user (UserId: 500) trying to delete a review
        var command = new DeleteReviewCommand(ReviewId: 1, UserId: 500, IsSuperAdmin: false, IsAdmin: true);

        // Game is owned by User 500
        var review = new Review { Id = 1, UserId = 100, GameId = 1, Game = new Game { OwnerId = 500 } };

        var reviewsList = new List<Review> { review };
        var mockReviews = reviewsList.BuildMockDbSet();
        _mockDbContext.Reviews.Returns(mockReviews);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockReviews.Received(1).Remove(review);
    }

    [Fact]
    public async Task DeleteReview_ShouldFail_WhenUserIsAdminButDoesNotOwnGame()
    {
        // Arrange
        var handler = new DeleteReviewCommandHandler(_mockDbContext, _mockCache);

        // Admin user (UserId: 501) trying to delete a review
        var command = new DeleteReviewCommand(ReviewId: 1, UserId: 501, IsSuperAdmin: false, IsAdmin: true);

        // Game is owned by User 500
        var review = new Review { Id = 1, UserId = 100, GameId = 1, Game = new Game { OwnerId = 500 } };

        var reviewsList = new List<Review> { review };
        var mockReviews = reviewsList.BuildMockDbSet();
        _mockDbContext.Reviews.Returns(mockReviews);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.Forbidden");
    }
}
using FluentAssertions;
using GameStore.Application.Features.Games.Queries;
using GameStore.Application.Interfaces;
using GameStore.Domain.Entities;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace GameStore.Application.Tests.Features;

public class ReviewQueryHandlerTests
{
    private readonly IApplicationDbContext _mockDbContext;

    public ReviewQueryHandlerTests()
    {
        _mockDbContext = Substitute.For<IApplicationDbContext>();
    }

    [Fact]
    public async Task GetGameReviews_ShouldReturnPaginatedList()
    {
        // Arrange
        var handler = new GetGameReviewsQueryHandler(_mockDbContext);
        var query = new GetGameReviewsQuery(GameId: 1, Page: 2, PageSize: 5); // Requesting page 2

        var user = new User { Id = 1, Name = "TestUser", Username = "TestUser", Email = "test@test.com" };

        // Generate 12 dummy reviews
        var reviewsList = Enumerable.Range(1, 12).Select(i => new Review
        {
            Id = i,
            GameId = 1,
            UserId = 1,
            User = user,
            Rating = 5,
            CreatedAt = DateTime.UtcNow.AddMinutes(-i) // Ordered by newest
        }).ToList();

        var mockReviews = reviewsList.BuildMockDbSet();
        _mockDbContext.Reviews.Returns(mockReviews);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var pagedList = result.Value;
        pagedList.TotalCount.Should().Be(12);
        pagedList.Page.Should().Be(2);
        pagedList.PageSize.Should().Be(5);
        pagedList.Items.Count.Should().Be(5); // Page 2 should have 5 items

        // Since there are 12 items total, and we took 10 across page 1 and 2, there are 2 items left for page 3
        pagedList.HasNextPage.Should().BeTrue();
    }
}
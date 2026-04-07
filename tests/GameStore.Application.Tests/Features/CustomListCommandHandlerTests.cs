using FluentAssertions;
using GameStore.Application.Features.Users.Commands;
using GameStore.Application.Interfaces;
using GameStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;
using MockQueryable.NSubstitute;

namespace GameStore.Application.Tests.Features;

public class CustomListCommandHandlerTests
{
    private readonly IApplicationDbContext _mockDbContext;

    public CustomListCommandHandlerTests()
    {
        _mockDbContext = Substitute.For<IApplicationDbContext>();
    }

    [Fact]
    public async Task CreateCustomList_ShouldAddList_AndReturnId()
    {
        // Arrange
        var handler = new CreateCustomListCommandHandler(_mockDbContext);
        var command = new CreateCustomListCommand(1, "My Wishlist");

        var mockDbSet = Substitute.For<DbSet<CustomList>>();
        _mockDbContext.CustomLists.Returns(mockDbSet);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockDbSet.Received(1).Add(Arg.Is<CustomList>(l => l.Name == "My Wishlist" && l.UserId == 1));
        await _mockDbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteCustomList_ShouldRemoveList_WhenUserIsOwner()
    {
        // Arrange
        var handler = new DeleteCustomListCommandHandler(_mockDbContext);
        var command = new DeleteCustomListCommand(UserId: 1, ListId: 5);

        var list = new CustomList { Id = 5, UserId = 1, Name = "My Wishlist" };

        // Mock IQueryable for FirstOrDefaultAsync
        var lists = new List<CustomList> { list }.AsQueryable();
        var mockDbSet = lists.BuildMockDbSet();

        _mockDbContext.CustomLists.Returns(mockDbSet);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockDbSet.Received(1).Remove(list);
        await _mockDbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
using FluentAssertions;
using GameStore.Application.Features.Games.Commands;
using GameStore.Application.Interfaces;
using GameStore.Domain.Entities;
using GameStore.Domain.Errors;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace GameStore.Application.Tests.Features;

public class DeleteGameCommandHandlerTests
{
    private readonly IApplicationDbContext _mockDbContext;
    private readonly DeleteGameCommandHandler _handler;

    public DeleteGameCommandHandlerTests()
    {
        _mockDbContext = Substitute.For<IApplicationDbContext>();
        _handler = new DeleteGameCommandHandler(_mockDbContext);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenGameDoesNotExist()
    {
        // Arrange: Command requests deletion of ID 999
        var command = new DeleteGameCommand(999);

        // Properly mock the DbSet so FindAsync returns null
        var mockDbSet = Substitute.For<DbSet<Game>, IQueryable<Game>>();
        mockDbSet.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<Game?>((Game?)null));

        _mockDbContext.Games.Returns(mockDbSet);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue("The handler should fail gracefully if the entity is missing.");
        result.Error.Should().Be(GameErrors.NotFound); // Validates your GameErrors.cs is used

        // Verify the database NEVER called SaveChangesAsync
        await _mockDbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
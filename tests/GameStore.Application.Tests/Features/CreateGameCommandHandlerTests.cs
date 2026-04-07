using FluentAssertions;
using GameStore.Application.Features.Games.Commands;
using GameStore.Application.Interfaces;
using GameStore.Domain.Entities;
using NSubstitute;
using Xunit; // <-- ADD THIS

namespace GameStore.Application.Tests.Features;

public class CreateGameCommandHandlerTests
{
    private readonly IApplicationDbContext _mockDbContext;
    private readonly CreateGameCommandHandler _handler;

    public CreateGameCommandHandlerTests()
    {
        _mockDbContext = Substitute.For<IApplicationDbContext>();
        _handler = new CreateGameCommandHandler(_mockDbContext);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccessResult_WhenCommandIsValid()
    {
        // Arrange: Updated to match your exact constructor signature
        // (int, string, string, string?, int, decimal?, DateOnly)
        var command = new CreateGameCommand(
            1,
            "Ghost of Yōtei",
            "Action RPG",
            "Sony",
            1,
            69.99m,
            new DateOnly(2025, 1, 1)
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();

        // Verify the database was actually called
        _mockDbContext.Received(1).Games.Add(Arg.Any<Game>());
        await _mockDbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
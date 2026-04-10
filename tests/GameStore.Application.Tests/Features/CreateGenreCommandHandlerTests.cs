using FluentAssertions;
using GameStore.Application.Features.Games.Commands;
using GameStore.Application.Interfaces;
using GameStore.Domain.Entities;
using Microsoft.Extensions.Caching.Hybrid;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace GameStore.Application.Tests.Features;

public class CreateGenreCommandHandlerTests
{
    private readonly IApplicationDbContext _mockDbContext;
    private readonly HybridCache _mockCache;

    public CreateGenreCommandHandlerTests()
    {
        _mockDbContext = Substitute.For<IApplicationDbContext>();
        _mockCache = Substitute.For<HybridCache>(); // Mock the cache
    }

    [Fact]
    public async Task Handle_ShouldCreateGenre_WhenNameIsUnique()
    {
        // Arrange
        var handler = new CreateGenreCommandHandler(_mockDbContext, _mockCache); // Pass the cache
        var command = new CreateGenreCommand("Strategy");

        // Set up an empty Genres DbSet
        var genresList = new List<Genre>();
        var mockGenres = genresList.BuildMockDbSet();
        _mockDbContext.Genres.Returns(mockGenres);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify that the Add method was called exactly once with the correct Genre name
        mockGenres.Received(1).Add(Arg.Is<Genre>(g => g.Name == "Strategy"));

        // Verify that the database changes were saved
        await _mockDbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenGenreNameAlreadyExists()
    {
        // Arrange
        var handler = new CreateGenreCommandHandler(_mockDbContext, _mockCache); // Pass the cache

        // Command attempts to create "RPG"
        var command = new CreateGenreCommand("RPG");

        // The database already contains "rpg" (lowercase) to test the case-insensitive logic
        var genresList = new List<Genre>
        {
            new Genre { Id = 1, Name = "rpg" }
        };

        var mockGenres = genresList.BuildMockDbSet();
        _mockDbContext.Genres.Returns(mockGenres);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Genre.Duplicate");

        // Verify that Add and SaveChangesAsync were NEVER called to protect the database
        mockGenres.DidNotReceive().Add(Arg.Any<Genre>());
        await _mockDbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
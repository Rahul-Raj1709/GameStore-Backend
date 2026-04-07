using FluentAssertions;
using GameStore.Domain.Entities;
using Xunit;

namespace GameStore.Application.Tests.Domain;

public class GameEntityTests
{
    [Fact]
    public void Game_ShouldInitializeWithDefaults_WhenCreated()
    {
        // Arrange & Act
        var game = new Game
        {
            Name = "Ghost of Yōtei"
        };

        // Assert
        // Validating the default values defined in your updated Game.cs entity
        game.AverageRating.Should().Be(0);
        game.LikedByUsers.Should().BeEmpty();

        // CreatedAt should be automatically set to roughly UTC Now
        game.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}
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
        // Utilizing your C# 11 'required' property and object initializer
        var game = new Game
        {
            Name = "Ghost of Yōtei"
        };

        // Assert
        // Validating the default values defined in your Game.cs entity
        game.TotalLikes.Should().Be(0);
        game.LikedByUsers.Should().BeEmpty();

        // AddedAt should be automatically set to roughly UTC Now
        game.AddedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}
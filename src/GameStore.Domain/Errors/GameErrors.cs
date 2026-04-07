using GameStore.Domain.Shared;

namespace GameStore.Domain.Errors;

public static class GameErrors
{
    public static readonly Error NotFound = new(
        "Game.NotFound",
        "The game with the specified ID was not found.");
}
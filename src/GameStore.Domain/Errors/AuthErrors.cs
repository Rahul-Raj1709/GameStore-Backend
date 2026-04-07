using GameStore.Domain.Shared;

namespace GameStore.Domain.Errors;

public static class AuthErrors
{
    public static readonly Error EmailAlreadyExists = new(
        "Auth.EmailExists", "The email provided is already registered.");

    public static readonly Error InvalidCredentials = new(
        "Auth.InvalidCredentials", "Invalid email or password.");
}
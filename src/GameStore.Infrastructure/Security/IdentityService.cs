using System.Security.Cryptography;
using GameStore.Application.Interfaces.Security;
using GameStore.Domain.Errors;
using GameStore.Domain.Shared;
using GameStore.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace GameStore.Infrastructure.Security;

public class IdentityService(UserManager<ApplicationUser> userManager) : IIdentityService
{
    public async Task<Result<(string IdentityId, string RefreshToken)>> RegisterUserAsync(string email, string username, string password, string role)
    {
        var user = new ApplicationUser { UserName = username, Email = email };
        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded) return Result.Failure<(string, string)>(new Error("Identity.RegistrationFailed", result.Errors.First().Description));

        await userManager.AddToRoleAsync(user, role);

        var refreshToken = GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Refresh token valid for 7 days
        await userManager.UpdateAsync(user);

        return Result.Success((user.Id, refreshToken));
    }

    public async Task<Result<(string IdentityId, string RefreshToken)>> AuthenticateUserAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null || !await userManager.CheckPasswordAsync(user, password))
            return Result.Failure<(string, string)>(AuthErrors.InvalidCredentials);

        var refreshToken = GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await userManager.UpdateAsync(user);

        return Result.Success((user.Id, refreshToken));
    }

    public async Task<Result<(string IdentityId, string NewRefreshToken)>> RefreshTokenAsync(string email, string currentRefreshToken)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user == null || user.RefreshToken != currentRefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return Result.Failure<(string, string)>(new Error("Auth.InvalidToken", "Invalid or expired refresh token."));
        }

        var newRefreshToken = GenerateRefreshToken();
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await userManager.UpdateAsync(user);

        return Result.Success((user.Id, newRefreshToken));
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
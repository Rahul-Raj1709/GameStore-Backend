using GameStore.Application.Interfaces.Security;
using GameStore.Domain.Errors;
using GameStore.Domain.Shared;
using GameStore.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;

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

    public async Task<Result> DeleteUserAsync(string identityId)
    {
        var user = await userManager.FindByIdAsync(identityId);
        if (user == null) return Result.Success(); // Idempotent

        var result = await userManager.DeleteAsync(user);
        return result.Succeeded ? Result.Success() : Result.Failure(new Error("Identity.DeleteFailed", "Failed to delete identity user."));
    }

    public async Task<Result<string>> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null) return Result.Failure<string>(AuthErrors.InvalidCredentials);

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        return Result.Success(token);
    }

    public async Task<Result> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null) return Result.Failure(AuthErrors.InvalidCredentials);

        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded) return Result.Failure(new Error("Identity.ResetFailed", result.Errors.First().Description));

        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(string email, string currentPassword, string newPassword)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return Result.Failure(new Error("Auth.UserNotFound", "User not found."));
        }

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (!result.Succeeded)
        {
            var errorMessages = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(new Error("Auth.ChangePasswordFailed", errorMessages));
        }

        return Result.Success();
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
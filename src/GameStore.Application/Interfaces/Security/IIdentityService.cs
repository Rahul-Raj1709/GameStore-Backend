using GameStore.Domain.Shared;

namespace GameStore.Application.Interfaces.Security;

public interface IIdentityService
{
    Task<Result<(string IdentityId, string RefreshToken)>> RegisterUserAsync(string email, string username, string password, string role);
    Task<Result<(string IdentityId, string RefreshToken)>> AuthenticateUserAsync(string email, string password);
    Task<Result<(string IdentityId, string NewRefreshToken)>> RefreshTokenAsync(string email, string currentRefreshToken);
    Task<Result> DeleteUserAsync(string identityId);
    Task<Result<string>> GeneratePasswordResetTokenAsync(string email);
    Task<Result> ResetPasswordAsync(string email, string token, string newPassword);
}
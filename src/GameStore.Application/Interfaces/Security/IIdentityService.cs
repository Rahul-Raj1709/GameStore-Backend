using GameStore.Domain.Shared;

namespace GameStore.Application.Interfaces.Security;

public interface IIdentityService
{
    // Now returns the IdentityId AND the generated Refresh Token
    Task<Result<(string IdentityId, string RefreshToken)>> RegisterUserAsync(string email, string username, string password, string role);
    Task<Result<(string IdentityId, string RefreshToken)>> AuthenticateUserAsync(string email, string password);

    // Validates an old token and issues a new one
    Task<Result<(string IdentityId, string NewRefreshToken)>> RefreshTokenAsync(string email, string currentRefreshToken);
}
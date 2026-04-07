using GameStore.Domain.Entities;

namespace GameStore.Application.Interfaces.Security;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
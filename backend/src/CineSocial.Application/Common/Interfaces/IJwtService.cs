using CineSocial.Domain.Entities.User;

namespace CineSocial.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateToken(AppUser user);
    Guid? ValidateToken(string token);
}

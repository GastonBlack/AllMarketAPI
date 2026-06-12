using AllMarket.Features.Users.Models;

namespace AllMarket.Features.Auth.Security;

public interface IJwtTokenGenerator
{
    JwtTokenResult GenerateToken(User user);
}

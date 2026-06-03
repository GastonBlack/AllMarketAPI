using System.Security.Claims;
using AllMarket.Infrastructure.Exceptions;

namespace AllMarket.Infrastructure.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetAuthenticatedUserId(this ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
            throw new UnauthorizedException("Authenticated user id was not found.");

        return userId;
    }
}

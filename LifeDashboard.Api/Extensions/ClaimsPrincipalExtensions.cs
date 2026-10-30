using System.Security.Claims;

namespace LifeDashboard.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(
        this ClaimsPrincipal user)
    {
        var userId =
            user.FindFirst(
                ClaimTypes.NameIdentifier)
            ?.Value;


        if (string.IsNullOrEmpty(userId))
        {
            throw new Exception(
                "Token 中沒有 UserId");
        }


        return Guid.Parse(userId);
    }
}
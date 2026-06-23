using System.Security.Claims;

namespace EventPlatform.API.Extensions
{
    public static class ClaimsExtensions
    {
        /// <summary>Gets the authenticated user's Guid from JWT sub claim</summary>
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? user.FindFirstValue("sub");
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }

        /// <summary>Gets the authenticated user's role from JWT</summary>
        public static string GetRole(this ClaimsPrincipal user)
            => user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        /// <summary>Gets the authenticated user's name from JWT</summary>
        public static string GetName(this ClaimsPrincipal user)
            => user.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        /// <summary>Returns null if not authenticated (for optional auth endpoints)</summary>
        public static Guid? GetUserIdOrNull(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? user.FindFirstValue("sub");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }
}

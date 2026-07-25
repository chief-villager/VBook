using Bookkeeping.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Bookkeeping.Infrastructure.Auth;

// Decides a PermissionRequirement per business: it reads the businessId from the
// route, finds the caller's role for that business from the token's business_role
// claims, and grants access if that role's permission set (RolePermissions.Map)
// contains the required permission. This is what makes the roles per-business — the
// same user may pass on one business and be denied on another.
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var http = _httpContextAccessor.HttpContext;
        if (http is null)
            return Task.CompletedTask;

        // The permission is always scoped to a business on the route.
        if (!http.Request.RouteValues.TryGetValue("businessId", out var raw)
            || !Guid.TryParse(raw?.ToString(), out var businessId))
            return Task.CompletedTask;

        foreach (var claim in context.User.FindAll(BookkeepingClaims.BusinessRole))
        {
            // Claim value is "{businessId}:{role}".
            var parts = claim.Value.Split(':', 2);
            if (parts.Length != 2)
                continue;
            if (!Guid.TryParse(parts[0], out var claimBusinessId) || claimBusinessId != businessId)
                continue;
            if (!Enum.TryParse<BusinessRole>(parts[1], out var role))
                continue;

            if (RolePermissions.Map.TryGetValue(role, out var permissions)
                && permissions.Contains(requirement.Permission))
            {
                context.Succeed(requirement);
            }

            // Only one membership per (business, user), so stop at the matching claim.
            break;
        }

        return Task.CompletedTask;
    }
}

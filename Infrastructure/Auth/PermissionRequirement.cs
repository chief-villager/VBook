using Microsoft.AspNetCore.Authorization;

namespace Bookkeeping.Infrastructure.Auth;

// An authorization requirement for a single permission (e.g. "Invoices.Create").
// The handler decides it against the caller's role for the business on the route.
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission) => Permission = permission;
}

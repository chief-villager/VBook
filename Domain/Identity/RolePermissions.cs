namespace Bookkeeping.Domain.Identity;

// The canonical catalog of what each business role may do. BusinessRole is the single
// source of truth for which roles exist; this map is the single source of truth for
// the permissions each one unlocks. The seeder builds the Identity role-claim catalog
// from here, and authorization reads it — so roles and their permissions can never
// drift from a hand-typed list.
public static class RolePermissions
{
    public static readonly IReadOnlyDictionary<BusinessRole, string[]> Map =
        new Dictionary<BusinessRole, string[]>
        {
            [BusinessRole.Owner] = new[]
            {
                Permissions.Invoices.Read,
                Permissions.Invoices.Create,
                Permissions.Invoices.Update,
                Permissions.Invoices.Delete,
                Permissions.Users.Read,
                Permissions.Users.Create,
                Permissions.Users.Delete,
            },
            [BusinessRole.Admin] = new[]
            {
                Permissions.Invoices.Read,
                Permissions.Invoices.Create,
            },
            [BusinessRole.Accountant] = new[]
            {
                Permissions.Invoices.Read,
            },
        };
}

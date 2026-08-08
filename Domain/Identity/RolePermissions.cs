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
            // The owner holds every capability for their business.
            [BusinessRole.Owner] = new[]
            {
                Permissions.Business.Read,
                Permissions.Business.Manage,
                Permissions.Users.Read,
                Permissions.Users.Create,
                Permissions.Users.Delete,
                Permissions.Invoices.Read,
                Permissions.Invoices.Create,
                Permissions.Invoices.Update,
                Permissions.Invoices.Delete,
                Permissions.Transactions.Read,
                Permissions.Transactions.Record,
                Permissions.Ledger.Read,
                Permissions.Reports.Read,
                Permissions.CreditReadiness.Read,
                Permissions.BankAccounts.Read,
                Permissions.BankAccounts.Manage,
                Permissions.BankImports.Read,
                Permissions.BankImports.Manage,
            },
            // Operational access: run the books and the bank feed, but no user
            // management and no invoice edit/delete.
            [BusinessRole.Admin] = new[]
            {
                Permissions.Business.Read,
                Permissions.Business.Manage,
                Permissions.Invoices.Read,
                Permissions.Invoices.Create,
                Permissions.Transactions.Read,
                Permissions.Transactions.Record,
                Permissions.Ledger.Read,
                Permissions.Reports.Read,
                Permissions.BankAccounts.Read,
                Permissions.BankAccounts.Manage,
                Permissions.BankImports.Read,
                Permissions.BankImports.Manage,
            },
            // Read-only across the financial picture.
            [BusinessRole.Accountant] = new[]
            {
                Permissions.Business.Read,
                Permissions.Invoices.Read,
                Permissions.Transactions.Read,
                Permissions.Ledger.Read,
                Permissions.Reports.Read,
                Permissions.CreditReadiness.Read,
            },
        };
}

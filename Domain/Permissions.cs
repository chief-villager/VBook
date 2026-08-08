namespace Bookkeeping.Domain;

// The canonical catalog of capability names. Each constant is used verbatim as an
// authorization policy name — [Authorize(Policy = Permissions.Invoices.Create)] — and
// RolePermissions.Map decides which business role unlocks it. Grouped by module to
// mirror the bounded contexts.
public static class Permissions
{
    public static class Business
    {
        public const string Read = "Business.Read";
        public const string Manage = "Business.Manage";
    }

    public static class Users
    {
        public const string Read = "Users.Read";
        public const string Create = "Users.Create";
        public const string Delete = "Users.Delete";
    }

    public static class Invoices
    {
        public const string Read = "Invoices.Read";
        public const string Create = "Invoices.Create";
        public const string Update = "Invoices.Update";
        public const string Delete = "Invoices.Delete";
    }

    public static class Transactions
    {
        public const string Read = "Transactions.Read";
        public const string Record = "Transactions.Record";
    }

    public static class Ledger
    {
        public const string Read = "Ledger.Read";
    }

    public static class Reports
    {
        public const string Read = "Reports.Read";
    }

    public static class CreditReadiness
    {
        public const string Read = "CreditReadiness.Read";
    }

    public static class BankAccounts
    {
        public const string Read = "BankAccounts.Read";
        public const string Manage = "BankAccounts.Manage";
    }

    public static class BankImports
    {
        public const string Read = "BankImports.Read";
        public const string Manage = "BankImports.Manage";
    }
}

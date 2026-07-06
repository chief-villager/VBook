using Bookkeeping.Domain.Common;

namespace Bookkeeping.Domain.Ledger;

// An account in the chart of accounts. The Type and StatementLine tags are what
// let Reporting group balances into statements without any accounting logic of its own.
public sealed class Account : Entity<AccountId>
{
    public string Code { get; private set; } = string.Empty;         // e.g. "1000"
    public string Name { get; private set; } = string.Empty;         // e.g. "Cash and bank"
    public AccountType Type { get; private set; }
    public string StatementLine { get; private set; } = string.Empty; // IFRS for SMEs grouping

    private Account() { }

    public Account(string code, string name, AccountType type, string statementLine)
    {
        Id = AccountId.New();
        Code = code;
        Name = name;
        Type = type;
        StatementLine = statementLine;
    }

    public bool IsDebitNormal => Type is AccountType.Asset or AccountType.Expense;
}

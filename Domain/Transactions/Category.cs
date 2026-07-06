using Bookkeeping.Domain.Common;

namespace Bookkeeping.Domain.Transactions;

// Plain-language grouping the MSME owner chooses from. It carries no accounting
// knowledge: mapping a category to debit/credit accounts is the Ledger's job.
public sealed class Category : Entity<CategoryId>
{
    public string Name { get; private set; } = string.Empty;
    public TransactionType Type { get; private set; }

    private Category() { }

    public Category(string name, TransactionType type)
    {
        Id = CategoryId.New();
        Name = name;
        Type = type;
    }
}

using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Ledger;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookkeeping.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts", "ledger");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Code).HasMaxLength(10).IsRequired();
        builder.Property(a => a.Name).HasMaxLength(128).IsRequired();
        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.StatementLine).HasMaxLength(64);

        builder.Ignore(a => a.IsDebitNormal);
        builder.HasIndex(a => a.Code).IsUnique();

        SeedChartOfAccounts(builder);
    }

    // The chart of accounts is shared reference data: one IFRS for SMEs aligned set,
    // seeded once for every business. Ids are fixed so EnsureCreated is deterministic.
    private static void SeedChartOfAccounts(EntityTypeBuilder<Account> builder)
    {
        static object Row(string code, string name, AccountType type, string statementLine) => new
        {
            Id = new AccountId(new Guid($"00000000-0000-0000-0000-0000{code}0000")),
            Code = code,
            Name = name,
            Type = type,
            StatementLine = statementLine,
        };

        builder.HasData(
            Row("1000", "Cash and bank",      AccountType.Asset,     "Current assets"),
            Row("1100", "Inventory",          AccountType.Asset,     "Current assets"),
            Row("1500", "Equipment",          AccountType.Asset,     "Non-current assets"),
            Row("2000", "Trade payables",     AccountType.Liability, "Current liabilities"),
            Row("2500", "Loans payable",      AccountType.Liability, "Non-current liabilities"),
            Row("3000", "Owner's capital",    AccountType.Equity,    "Equity"),
            Row("3100", "Retained earnings",  AccountType.Equity,    "Equity"),
            Row("4000", "Sales revenue",      AccountType.Income,    "Revenue"),
            Row("4100", "Other income",       AccountType.Income,    "Revenue"),
            Row("5000", "General expenses",   AccountType.Expense,   "Operating expenses"),
            Row("5100", "Cost of goods sold", AccountType.Expense,   "Cost of sales"),
            Row("5200", "Rent",               AccountType.Expense,   "Operating expenses"));
    }
}

public sealed class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("journal_entries", "ledger");
        builder.HasKey(j => j.Id);
        builder.Ignore(j => j.DomainEvents);

        builder.Property(j => j.Narrative).HasMaxLength(256);
        builder.Property(j => j.SourceTransactionId)
            .HasConversion(new NullableTransactionIdConverter());

        // Read the collection through its backing field, since Lines is read-only.
        builder.Navigation(j => j.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Posting lines are owned: they have no identity outside their entry and
        // live in their own table with a shadow primary key.
        builder.OwnsMany(j => j.Lines, line =>
        {
            line.ToTable("posting_lines", "ledger");
            line.WithOwner().HasForeignKey("JournalEntryId");
            line.Property<int>("Id");
            line.HasKey("Id");

            line.Property(l => l.Debit).HasPrecision(18, 2);
            line.Property(l => l.Credit).HasPrecision(18, 2);
            line.Property(l => l.AccountId).IsRequired();
        });

        builder.HasIndex(j => new { j.BusinessId, j.Date });
    }
}

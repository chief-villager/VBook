using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookkeeping.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories", "transactions");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(128).IsRequired();
        builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(20);

        SeedDefaultCategories(builder);
    }

   

    // Shared reference categories, seeded once so any owner can record straight away.
    // Ids are fixed so EnsureCreated is deterministic and transactions can reference them.
    private static void SeedDefaultCategories(EntityTypeBuilder<Category> builder)
    {
        static object Row(int n, string name, TransactionType type) => new
        {
            Id = new CategoryId(new Guid($"00000000-0000-0000-0002-{n:D12}")),
            Name = name,
            Type = type,
        };

        builder.HasData(
            Row(1, "Sales",        TransactionType.Income),
            Row(2, "Other income", TransactionType.Income),
            Row(3, "Purchases",    TransactionType.Expense),
            Row(4, "Rent",         TransactionType.Expense),
            Row(5, "Transport",    TransactionType.Expense),
            Row(6, "Utilities",    TransactionType.Expense),
            Row(7, "Wages",        TransactionType.Expense),
            Row(8, "Owner investment", TransactionType.Capital),
            Row(9, "Bank loan",        TransactionType.Loan));
    }
}

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions", "transactions");
        builder.HasKey(t => t.Id);
        builder.Ignore(t => t.DomainEvents);

        builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Amount).HasPrecision(18, 2);
        builder.Property(t => t.Note).HasMaxLength(512);

        // CategoryId is within this module, so a real FK is appropriate.
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // BusinessId crosses into Identity: value only, no FK.
        builder.HasIndex(t => new { t.BusinessId, t.OccurredOn });
    }
}

using Bookkeeping.Application.Abstractions;
using Bookkeeping.Application.Common;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Identity;
using Bookkeeping.Domain.Invoices;
using Bookkeeping.Domain.Ledger;
using Bookkeeping.Domain.Transactions;
using Bookkeeping.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bookkeeping.Infrastructure.Persistence;

// A single DbContext for the whole application, with one schema per module so the
// boundaries are visible at the data layer. Cross-module references are plain id
// values with no foreign key constraint; integrity across modules is enforced in
// the application, not the database.
//
// It also backs ASP.NET Core Identity (auth schema): authentication is a supporting
// concern that shares this unit of work, so a domain User and its credentials commit
// in one transaction. The domain model itself is untouched by Identity.
public sealed class AppDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IUnitOfWork
{
    private readonly IDomainEventDispatcher _dispatcher;

    public AppDbContext(DbContextOptions<AppDbContext> options, IDomainEventDispatcher dispatcher)
        : base(options)
    {
        _dispatcher = dispatcher;
    }

    // `Users` is already defined by IdentityDbContext (DbSet<ApplicationUser>), so the
    // domain user set is exposed under a distinct name.
    public DbSet<User> DomainUsers => Set<User>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BusinessMembership> Memberships => Set<BusinessMembership>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<StagedBankTransaction> StagedBankTransactions => Set<StagedBankTransaction>();
    public DbSet<LinkedBankAccount> LinkedBankAccounts => Set<LinkedBankAccount>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoicePdfJob> InvoicePdfJobs => Set<InvoicePdfJob>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<UserId>().HaveConversion<UserIdConverter>();
        builder.Properties<BusinessId>().HaveConversion<BusinessIdConverter>();
        builder.Properties<TransactionId>().HaveConversion<TransactionIdConverter>();
        builder.Properties<CategoryId>().HaveConversion<CategoryIdConverter>();
        builder.Properties<StagedTransactionId>().HaveConversion<StagedTransactionIdConverter>();
        builder.Properties<LinkedBankAccountId>().HaveConversion<LinkedBankAccountIdConverter>();
        builder.Properties<MembershipId>().HaveConversion<MembershipIdConverter>();
        builder.Properties<AccountId>().HaveConversion<AccountIdConverter>();
        builder.Properties<JournalEntryId>().HaveConversion<JournalEntryIdConverter>();
        builder.Properties<InvoiceId>().HaveConversion<InvoiceIdConverter>();
        builder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Required now that we derive from IdentityDbContext: this models the AspNet*
        // tables. Keep them in their own `auth` schema so module boundaries stay visible.
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ApplicationUser>().ToTable("users", "auth");
        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("roles", "auth");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles", "auth");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims", "auth");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins", "auth");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens", "auth");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims", "auth");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    // Overriding SaveChanges is what makes domain events part of the same
    // transaction as the change that raised them.
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken ct)
    {
        // Loop so that events raised by handlers (rare) are also dispatched.
        while (true)
        {
            var aggregates = ChangeTracker
                .Entries<IHasDomainEvents>()
                .Where(e => e.Entity.DomainEvents.Count > 0)
                .Select(e => e.Entity)
                .ToList();
            if (aggregates.Count == 0)
                break;

            var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();
            foreach (var aggregate in aggregates)
                aggregate.ClearDomainEvents();

            await _dispatcher.DispatchAsync(domainEvents, ct);
        }
    }
}

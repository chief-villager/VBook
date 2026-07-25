using Bookkeeping.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookkeeping.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "identity");
        builder.HasKey(u => u.Id);
        builder.Ignore(u => u.DomainEvents);

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(256);
        builder.HasIndex(u => u.Email).IsUnique();
    }
}

public sealed class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        builder.ToTable("businesses", "identity");
        builder.HasKey(b => b.Id);
        builder.Ignore(b => b.DomainEvents);

        builder.Property(b => b.Name).HasMaxLength(256).IsRequired();
        builder.Property(b => b.Sector).HasConversion<string>().HasMaxLength(30);

        // OwnerId points at identity.users but is stored as a plain value.
        // No cross-table foreign key is declared here on purpose.
        builder.Property(b => b.OwnerId).IsRequired();
        builder.HasIndex(b => b.OwnerId);
    }
}

public sealed class BusinessMembershipConfiguration : IEntityTypeConfiguration<BusinessMembership>
{
    public void Configure(EntityTypeBuilder<BusinessMembership> builder)
    {
        builder.ToTable("memberships", "identity");
        builder.HasKey(m => m.Id);
        builder.Ignore(m => m.DomainEvents);

        // BusinessId and UserId are plain cross-aggregate values (no FK), matching
        // Business.OwnerId. A user can hold at most one membership per business.
        builder.Property(m => m.BusinessId).IsRequired();
        builder.Property(m => m.UserId).IsRequired();
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(m => new { m.BusinessId, m.UserId }).IsUnique();
    }
}

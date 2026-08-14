using Bookkeeping.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookkeeping.Infrastructure.Persistence.Configurations;

// Refresh tokens sit with the other authentication tables in the `auth` schema.
// UserId is the shared Guid link to ApplicationUser (value only, matching how the
// rest of the model crosses that boundary), so no FK is declared here.
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens", "auth");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();

        // Rotation and reuse-detection look tokens up by hash; family revocation
        // (logout / theft response) sweeps every row sharing a family.
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.FamilyId);
        builder.HasIndex(t => t.UserId);
    }
}

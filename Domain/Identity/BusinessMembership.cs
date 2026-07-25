using Bookkeeping.Domain.Common;

namespace Bookkeeping.Domain.Identity;

// The roles a user can hold within a single business. Roles are per-business, not
// global: the same person may own one business and merely view another. Owner is
// assigned once, to the user who registers the business, and is never granted
// through the add-member flow.
public enum BusinessRole
{
    Owner,
    Admin,
    Accountant,
}

// The membership relationship between a User and a Business, carrying the user's
// role there. This is the source of truth for "what is this person to this
// business" — a domain fact that business rules and authorization decisions read.
// Both ids are plain cross-aggregate values with no database foreign key,
// consistent with the rest of the model.
public sealed class BusinessMembership : AggregateRoot<MembershipId>
{
    public BusinessId BusinessId { get; private set; }
    public UserId UserId { get; private set; }
    public BusinessRole Role { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }

    private BusinessMembership() { }

    private BusinessMembership(BusinessId businessId, UserId userId, BusinessRole role)
    {
        Id = MembershipId.New();
        BusinessId = businessId;
        UserId = userId;
        Role = role;
        JoinedAt = DateTimeOffset.UtcNow;
    }

    public static BusinessMembership Create(BusinessId businessId, UserId userId, BusinessRole role)
        => new(businessId, userId, role);

    public Result ChangeRole(BusinessRole role)
    {
        if (Role == BusinessRole.Owner)
            return Result.Failure("The owner's role cannot be changed.");
        if (role == BusinessRole.Owner)
            return Result.Failure("Ownership cannot be granted through a role change.");

        Role = role;
        return Result.Success();
    }
}

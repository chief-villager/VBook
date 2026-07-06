using Bookkeeping.Domain.Common;

namespace Bookkeeping.Domain.Identity;

public sealed class User : AggregateRoot<UserId>
{
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;

    private User() { }

    public User(string email, string displayName)
    {
        Id = UserId.New();
        Email = email;
        DisplayName = displayName;
    }
}

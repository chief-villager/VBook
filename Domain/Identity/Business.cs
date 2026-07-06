using Bookkeeping.Domain.Common;

namespace Bookkeeping.Domain.Identity;

public sealed class Business : AggregateRoot<BusinessId>
{
    public UserId OwnerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public BusinessSector Sector { get; private set; }

    private Business() { }

    private Business(UserId ownerId, string name, BusinessSector sector)
    {
        Id = BusinessId.New();
        OwnerId = ownerId;
        Name = name;
        Sector = sector;
    }

    public static Business Register(UserId ownerId, string name, BusinessSector sector)
        => new(ownerId, name, sector);
}

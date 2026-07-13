using Bookkeeping.Domain.Common;

namespace Bookkeeping.Domain.Identity;

public sealed class Business : AggregateRoot<BusinessId>
{
    public UserId OwnerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public BusinessSector Sector { get; private set; }
    public InvoiceTemplate? Template {get; private set;}

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

   public Result SetTemplate(string logoUrl, string businessName, string accountNumber,
        string bankName, string terms)
    {
        if (string.IsNullOrWhiteSpace(logoUrl))
            return Result.Failure("logo url cannot be empty");
        if (string.IsNullOrWhiteSpace(businessName))
            return Result.Failure("business name cannot be empty");

        Template = new InvoiceTemplate(Id, logoUrl, businessName, accountNumber, bankName, terms);
        return Result.Success();
    }

    public bool HasTemplate => Template is not null;
}

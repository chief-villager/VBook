namespace Bookkeeping.Domain.Common;

// Marks an aggregate that belongs to exactly one business. Lets application services
// verify that a resource named by its own id actually belongs to the business in the
// request route before acting on it — the resource-scoping half of authorization
// (route-level permission checks are the other half). See ResourceOwnership.
public interface IBusinessScoped
{
    BusinessId BusinessId { get; }
}

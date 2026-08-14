using Bookkeeping.Domain.Common;

namespace Bookkeeping.Application.Common;

// Resource-scoped authorization guard, closing the IDOR gap. Route-level permission
// checks prove the caller may act on the business in the route; this proves the
// *resource* they named actually belongs to that business. A missing resource and one
// owned by another business are reported identically ("not found") so a caller can't
// probe for the existence of resources outside their own business.
public static class ResourceOwnership
{
    // Returns the resource wrapped in a success result when it exists and belongs to the
    // business; otherwise a "not found" failure. The success value is non-null.
    public static Result<T> RequireOwned<T>(T? resource, BusinessId businessId, string resourceName)
        where T : class, IBusinessScoped
        => resource is not null && resource.BusinessId == businessId
            ? Result<T>.Success(resource)
            : Result<T>.Failure($"{resourceName} not found.");
}

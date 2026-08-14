namespace Bookkeeping.Application.Common;

// A single page of results plus the metadata a client needs to page through the
// rest. TotalCount is the full count across all pages, not just this slice.
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

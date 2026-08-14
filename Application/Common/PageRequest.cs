namespace Bookkeeping.Application.Common;

// Normalised paging parameters. Page is 1-based. Values are clamped so a caller
// can't request page 0/negative, a non-positive size, or an unbounded page that
// would pull the whole table.
public sealed class PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int Page { get; }
    public int PageSize { get; }

    public PageRequest(int page = 1, int pageSize = DefaultPageSize)
    {
        Page = page < 1 ? 1 : page;
        PageSize = pageSize < 1 ? DefaultPageSize
            : pageSize > MaxPageSize ? MaxPageSize
            : pageSize;
    }

    // Rows to skip to reach this page.
    public int Skip => (Page - 1) * PageSize;
}

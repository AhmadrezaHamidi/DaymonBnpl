using System.ComponentModel.DataAnnotations;

namespace BnplService.Application.Handler.Admin.Messages.Queries.SearchPageMessages;

public sealed record SearchPageMessagesQuery : BnplPagedQuery<BnplMessageResponse<BnplPagedResponse<PageMessageDto>>>
{
    [StringLength(60)] public string? PageKey { get; init; }
    public bool? IsActive { get; init; }
}

public sealed class SearchPageMessagesQueryHandler(IBnplDbContext db, TimeProvider time)
    : BnplHandlerBase(db, time), IRequestHandler<SearchPageMessagesQuery, BnplMessageResponse<BnplPagedResponse<PageMessageDto>>>
{
    public async Task<BnplMessageResponse<BnplPagedResponse<PageMessageDto>>> Handle(SearchPageMessagesQuery query, CancellationToken ct)
    {
        var (page, size) = Paging(query.Page, query.PageSize);
        var q = Db.PageMessages.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.PageKey))
        {
            var key = query.PageKey.Trim().ToLowerInvariant();
            q = q.Where(m => m.PageKey.Contains(key));
        }
        if (query.IsActive is { } active) q = q.Where(m => m.IsActive == active);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(m => m.Id).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return Ok(Page(items.Select(PageMessageDto.Of).ToList(), total, page, size));
    }
}

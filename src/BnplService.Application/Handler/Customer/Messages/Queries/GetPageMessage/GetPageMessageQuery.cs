using System.ComponentModel.DataAnnotations;

namespace BnplService.Application.Handler.Customer.Messages.Queries.GetPageMessage;

/// <summary>
/// «باکس پیام» یک صفحه (US-5). پاسخ null یعنی باکس نمایش داده نشود.
/// اگر چند پیام فعال باشد، جدیدترین نمایش داده می‌شود. خطای این درخواست نباید صفحه را از کار بیندازد (سمت اپ).
/// </summary>
public sealed record GetPageMessageQuery : BnplBaseQuery<BnplMessageResponse<PageMessageViewDto?>>
{
    [Required, StringLength(60)] public string PageKey { get; init; } = null!;
}

public sealed class GetPageMessageQueryHandler(IBnplDbContext db, TimeProvider time)
    : BnplHandlerBase(db, time), IRequestHandler<GetPageMessageQuery, BnplMessageResponse<PageMessageViewDto?>>
{
    public async Task<BnplMessageResponse<PageMessageViewDto?>> Handle(GetPageMessageQuery query, CancellationToken ct)
    {
        var key = PageMessage.NormalizeKey(query.PageKey);
        if (key is null) return Ok<PageMessageViewDto?>(null);
        var now = Now;
        var message = await Db.PageMessages.AsNoTracking()
            .Where(m => m.PageKey == key && m.IsActive && (m.ValidFrom == null || m.ValidFrom <= now) && (m.ValidTo == null || m.ValidTo >= now))
            .OrderByDescending(m => m.Id).FirstOrDefaultAsync(ct);
        return Ok(message is null ? null : new PageMessageViewDto(message.Text, message.Kind, message.Color));
    }
}

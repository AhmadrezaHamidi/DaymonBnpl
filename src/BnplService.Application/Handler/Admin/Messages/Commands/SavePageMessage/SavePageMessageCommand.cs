using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using BnplService.Application.Handler.Admin.Shared;

namespace BnplService.Application.Handler.Admin.Messages.Commands.SavePageMessage;

/// <summary>ساخت (Id = null) یا ویرایش پیام یک صفحه (US-5). کلید صفحه پس از ساخت عوض نمی‌شود.</summary>
public sealed record SavePageMessageCommand : BnplBaseCommand<BnplMessageResponse<PageMessageDto>>
{
    [JsonIgnore, BindNever] public int? Id { get; init; }

    /// <summary>فقط برای ساخت، مثل bnpl.new-request.</summary>
    [StringLength(60)] public string? PageKey { get; init; }

    [Required, StringLength(500, MinimumLength = 3)] public string Text { get; init; } = null!;
    public PageMessageKind Kind { get; init; } = PageMessageKind.Guide;

    /// <summary>#RRGGBB؛ خالی یعنی رنگ پیش‌فرض نوع پیام.</summary>
    [StringLength(7)] public string? Color { get; init; }

    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public bool IsActive { get; init; } = true;
    [StringLength(20)] public string? RowVersion { get; init; }
}

public sealed class SavePageMessageCommandHandler(IBnplDbContext db, IBnplCurrentUser user, TimeProvider time)
    : AdminHandlerBase(db, user, time), IRequestHandler<SavePageMessageCommand, BnplMessageResponse<PageMessageDto>>
{
    public async Task<BnplMessageResponse<PageMessageDto>> Handle(SavePageMessageCommand c, CancellationToken ct)
    {
        PageMessage message;
        if (c.Id is { } id)
        {
            message = await Db.PageMessages.FirstOrDefaultAsync(m => m.Id == id, ct) ?? throw BnplDomainException.NotFound("پیام یافت نشد.");
            message.EnsureRowVersion(c.RowVersion);
            message.Update(c.Text, c.Kind, c.Color, c.ValidFrom, c.ValidTo, c.IsActive);
        }
        else
        {
            message = new PageMessage(c.PageKey ?? "", c.Text, c.Kind, c.Color, c.ValidFrom, c.ValidTo, Now);
            if (!c.IsActive) message.Update(c.Text, c.Kind, c.Color, c.ValidFrom, c.ValidTo, false);
            Db.PageMessages.Add(message);
        }
        await Db.SaveChangesAsync(ct);
        return Ok(PageMessageDto.Of(message), "پیام ذخیره شد.");
    }
}

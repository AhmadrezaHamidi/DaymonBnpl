using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using BnplService.Application.Handler.Admin.Shared;

namespace BnplService.Application.Handler.Admin.Discounts.Commands.SaveDiscount;

/// <summary>ساخت (Id = null) یا ویرایش کد تخفیف. خودِ Code پس از ساخت عوض نمی‌شود.</summary>
public sealed record SaveDiscountCommand : BnplBaseCommand<BnplMessageResponse<DiscountCodeDto>>
{
    [JsonIgnore, BindNever] public int? Id { get; init; }

    /// <summary>فقط برای ساخت: ۳ تا ۳۰ حرف لاتین، عدد، - یا _.</summary>
    [StringLength(30)] public string? Code { get; init; }

    [Required, StringLength(100, MinimumLength = 2)] public string Title { get; init; } = null!;

    /// <summary>درصد از کل کارمزد قرارداد؛ یا این یا FixedAmount.</summary>
    [Range(0, 100)] public decimal? FeePercent { get; init; }

    [Range(1, long.MaxValue)] public long? FixedAmount { get; init; }
    [Range(1, long.MaxValue)] public long? MaxAmount { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    [Range(1, int.MaxValue)] public int? UsageLimit { get; init; }
    public bool IsActive { get; init; } = true;
    [StringLength(20)] public string? RowVersion { get; init; }
}

public sealed class SaveDiscountCommandHandler(IBnplDbContext db, IBnplCurrentUser user, TimeProvider time)
    : AdminHandlerBase(db, user, time), IRequestHandler<SaveDiscountCommand, BnplMessageResponse<DiscountCodeDto>>
{
    public async Task<BnplMessageResponse<DiscountCodeDto>> Handle(SaveDiscountCommand c, CancellationToken ct)
    {
        DiscountCode discount;
        if (c.Id is { } id)
        {
            discount = await Db.DiscountCodes.FirstOrDefaultAsync(d => d.Id == id, ct) ?? throw BnplDomainException.NotFound("کد تخفیف یافت نشد.");
            discount.EnsureRowVersion(c.RowVersion);
            discount.Update(c.Title, c.FeePercent, c.FixedAmount, c.MaxAmount, c.ValidFrom, c.ValidTo, c.UsageLimit, c.IsActive);
        }
        else
        {
            discount = new DiscountCode(c.Code ?? "", c.Title, c.FeePercent, c.FixedAmount, c.MaxAmount, c.ValidFrom, c.ValidTo, c.UsageLimit, Now);
            if (await Db.DiscountCodes.AnyAsync(d => d.Code == discount.Code, ct)) throw new BnplDomainException("این کد تخفیف قبلاً تعریف شده است.");
            if (!c.IsActive) discount.Update(c.Title, c.FeePercent, c.FixedAmount, c.MaxAmount, c.ValidFrom, c.ValidTo, c.UsageLimit, false);
            Db.DiscountCodes.Add(discount);
        }
        await Db.SaveChangesAsync(ct);
        return Ok(DiscountCodeDto.Of(discount), "کد تخفیف ذخیره شد.");
    }
}

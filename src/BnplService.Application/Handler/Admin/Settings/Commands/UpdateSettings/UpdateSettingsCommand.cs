using System.ComponentModel.DataAnnotations;
using BnplService.Application.Handler.Admin.Shared;

namespace BnplService.Application.Handler.Admin.Settings.Commands.UpdateSettings;

/// <summary>ویرایش تنظیمات سراسری. روی درخواست‌های ثبت‌شده اثر ندارد (درصدها و مهلت‌ها روی درخواست کپی شده‌اند).</summary>
public sealed record UpdateSettingsCommand : BnplBaseCommand<BnplMessageResponse<BnplSettingsDto>>
{
    public bool IsEnabled { get; init; }
    [Range(1, long.MaxValue)] public long MinAmount { get; init; }
    [Range(1, long.MaxValue)] public long MaxAmount { get; init; }
    [Range(1, long.MaxValue)] public long AmountStep { get; init; }
    [Range(1, 720)] public int CompletionHours { get; init; }
    [Range(5, 1440)] public int ActivationMinutes { get; init; }
    [Range(5, 120)] public int GatewayPaymentMinutes { get; init; }
    [Range(1, 100)] public int MaxActiveContractsPerCustomer { get; init; }
    /// <summary>درصد وثیقه از مبلغ پیش‌خرید (۱۰۰ = توکن هم‌ارزش مبلغ).</summary>
    [Range(1, 500)] public decimal CollateralPercent { get; init; } = 100;
    [Range(1, 20)] public int MaxIncompleteRequestsPerCustomer { get; init; } = 3;
    /// <summary>مبلغ‌های پیشنهادی صفحهٔ درخواست؛ null یعنی بدون تغییر.</summary>
    [MaxLength(10)] public IReadOnlyList<long>? SuggestedAmounts { get; init; }
    [Required, StringLength(20)] public string RowVersion { get; init; } = null!;
}

public sealed class UpdateSettingsCommandHandler(IBnplDbContext db, IBnplCurrentUser user, TimeProvider time)
    : AdminHandlerBase(db, user, time), IRequestHandler<UpdateSettingsCommand, BnplMessageResponse<BnplSettingsDto>>
{
    public async Task<BnplMessageResponse<BnplSettingsDto>> Handle(UpdateSettingsCommand c, CancellationToken ct)
    {
        var settings = await SettingsAsync(ct);
        settings.EnsureRowVersion(c.RowVersion);
        settings.Update(c.IsEnabled, c.MinAmount, c.MaxAmount, c.AmountStep, c.CompletionHours, c.ActivationMinutes, c.GatewayPaymentMinutes,
            c.MaxActiveContractsPerCustomer, c.CollateralPercent, c.MaxIncompleteRequestsPerCustomer,
            c.SuggestedAmounts ?? settings.SuggestedAmountList, Me, Now);
        await Db.SaveChangesAsync(ct);
        return Ok(BnplSettingsDto.Of(settings), "تنظیمات ذخیره شد.");
    }
}

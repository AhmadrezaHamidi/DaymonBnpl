namespace BnplService.Contracts.Settings;

public sealed record BnplSettingsDto(
    bool IsEnabled, long MinAmount, long MaxAmount, long AmountStep, int CompletionHours, int ActivationMinutes,
    int GatewayPaymentMinutes, int MaxActiveContractsPerCustomer, decimal CollateralPercent, int MaxIncompleteRequestsPerCustomer,
    IReadOnlyList<long> SuggestedAmounts, DateTime? UpdatedAt, string? UpdatedBy, string RowVersion)
{
    public static BnplSettingsDto Of(BnplSettings s) => new(s.IsEnabled, s.MinAmount, s.MaxAmount, s.AmountStep, s.CompletionHours,
        s.ActivationMinutes, s.GatewayPaymentMinutes, s.MaxActiveContractsPerCustomer, s.CollateralPercent,
        s.MaxIncompleteRequestsPerCustomer, s.SuggestedAmountList, s.UpdatedAt, s.UpdatedBy?.Name, s.RowVersion);
}

public sealed record PlanDto(int Id, string Title, int InstallmentCount, decimal FeePercent, decimal UpfrontFeePercent,
    decimal LatePenaltyDailyPercent, bool IsActive, int SortOrder, string RowVersion)
{
    public static PlanDto Of(BnplPlan p) => new(p.Id, p.Title, p.InstallmentCount, p.FeePercent, p.UpfrontFeePercent,
        p.LatePenaltyDailyPercent, p.IsActive, p.SortOrder, p.RowVersion);
}

public sealed record DiscountCodeDto(int Id, string Code, string Title, decimal? FeePercent, long? FixedAmount, long? MaxAmount,
    DateTime? ValidFrom, DateTime? ValidTo, int? UsageLimit, int UsedCount, bool IsActive, DateTime CreatedAt, string RowVersion)
{
    public static DiscountCodeDto Of(DiscountCode d) => new(d.Id, d.Code, d.Title, d.FeePercent, d.FixedAmount, d.MaxAmount,
        d.ValidFrom, d.ValidTo, d.UsageLimit, d.UsedCount, d.IsActive, d.CreatedAt, d.RowVersion);
}

/// <summary>پیام صفحه در پنل ادمین.</summary>
public sealed record PageMessageDto(int Id, string PageKey, string Text, PageMessageKind Kind, string? Color, DateTime? ValidFrom,
    DateTime? ValidTo, bool IsActive, DateTime CreatedAt, string RowVersion)
{
    public static PageMessageDto Of(PageMessage m) => new(m.Id, m.PageKey, m.Text, m.Kind, m.Color, m.ValidFrom, m.ValidTo, m.IsActive,
        m.CreatedAt, m.RowVersion);
}

/// <summary>«باکس پیام» یک صفحه در اپ (US-5)؛ null یعنی باکس نمایش داده نشود.</summary>
public sealed record PageMessageViewDto(string Text, PageMessageKind Kind, string? Color);

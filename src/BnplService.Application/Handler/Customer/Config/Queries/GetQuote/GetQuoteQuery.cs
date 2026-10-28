using System.ComponentModel.DataAnnotations;

namespace BnplService.Application.Handler.Customer.Config.Queries.GetQuote;

/// <summary>محاسبهٔ زندهٔ «مبلغ هر قسط / تعداد توکن / شرایط وام» با حرکت اسلایدر. چیزی ذخیره نمی‌شود.</summary>
public sealed record GetQuoteQuery : BnplBaseQuery<BnplMessageResponse<QuoteDto>>
{
    [Range(1, long.MaxValue)] public long Amount { get; init; }

    /// <summary>خالی ← اولین طرح فعال.</summary>
    public int? PlanId { get; init; }

    [StringLength(30)] public string? DiscountCode { get; init; }
}

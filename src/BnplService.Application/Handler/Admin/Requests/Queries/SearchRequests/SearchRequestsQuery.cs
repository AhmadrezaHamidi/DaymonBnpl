using System.ComponentModel.DataAnnotations;

namespace BnplService.Application.Handler.Admin.Requests.Queries.SearchRequests;

/// <summary>فهرست درخواست‌ها/قراردادها در پنل با فیلتر وضعیت، موبایل/شماره قرارداد/نام، معوق و بازهٔ تاریخ ثبت.</summary>
public sealed record SearchRequestsQuery : BnplPagedQuery<BnplMessageResponse<BnplPagedResponse<AdminRequestListItemDto>>>
{
    public BnplRequestStatus? Status { get; init; }

    /// <summary>موبایل، شماره قرارداد یا نام مشتری.</summary>
    [StringLength(50)] public string? Term { get; init; }

    /// <summary>فقط قراردادهای فعالی که قسط سررسیدگذشته دارند.</summary>
    public bool OverdueOnly { get; init; }

    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}

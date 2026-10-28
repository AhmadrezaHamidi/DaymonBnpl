namespace BnplService.Application.Handler.Customer.Requests.Queries.SearchMyRequests;

public enum MyRequestsFilter : byte { All = 0, Unsettled = 1, Settled = 2 }

/// <summary>«تاریخچه درخواست‌ها» با تب‌های همه / تسویه نشده / تسویه شده.</summary>
public sealed record SearchMyRequestsQuery : BnplPagedQuery<BnplMessageResponse<BnplPagedResponse<BnplRequestListItemDto>>>
{
    public MyRequestsFilter Filter { get; init; } = MyRequestsFilter.All;
}
